using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Primitives.Messaging.Abstractions;
using Primitives.Messaging.Extensions;
using Primitives.Messaging.Internal;
using Primitives.Messaging.Models;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Primitives.Messaging.Tests;

// Top-level so NSubstitute's Castle proxy can access the generic type argument
internal sealed record OrderCreated(int OrderId);

public sealed class InMemoryMessagePublisherTests
{
    private static IServiceProvider BuildProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddPrimitivesMessaging();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task PublishAsync_NoHandlers_DoesNotThrow()
    {
        var publisher = BuildProvider().GetRequiredService<IMessagePublisher>();
        await publisher.PublishAsync(new OrderCreated(1));
    }

    [Fact]
    public async Task PublishAsync_WithHandler_HandlerIsCalled()
    {
        var handler = Substitute.For<IMessageHandler<OrderCreated>>();
        handler.HandleAsync(Arg.Any<MessageContext<OrderCreated>>(), Arg.Any<CancellationToken>())
               .Returns(ConsumeResult.Ack);

        var provider = BuildProvider(svc =>
            svc.AddTransient<IMessageHandler<OrderCreated>>(_ => handler));

        await provider.GetRequiredService<IMessagePublisher>().PublishAsync(new OrderCreated(42));

        await handler.Received(1).HandleAsync(
            Arg.Is<MessageContext<OrderCreated>>(c => c.Message.OrderId == 42),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_PopulatesMetadata()
    {
        MessageMetadata? captured = null;

        var handler = Substitute.For<IMessageHandler<OrderCreated>>();
        handler.HandleAsync(Arg.Any<MessageContext<OrderCreated>>(), Arg.Any<CancellationToken>())
               .Returns(ci =>
               {
                   captured = ci.Arg<MessageContext<OrderCreated>>().Metadata;
                   return ConsumeResult.Ack;
               });

        var provider = BuildProvider(svc =>
            svc.AddTransient<IMessageHandler<OrderCreated>>(_ => handler));

        await provider.GetRequiredService<IMessagePublisher>().PublishAsync(new OrderCreated(1));

        Assert.NotNull(captured);
        Assert.NotEmpty(captured.MessageId);
        Assert.InRange(
            captured.Timestamp,
            DateTimeOffset.UtcNow.AddSeconds(-5),
            DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.Equal(1, captured.DeliveryCount);
    }

    [Fact]
    public async Task AddMessageHandler_RegistersHandlerViaDI_AndHandlerIsCalled()
    {
        var collected = new List<int>();

        var provider = BuildProvider(svc =>
        {
            svc.AddSingleton(collected);
            svc.AddMessageHandler<CollectingHandler, OrderCreated>("orders");
        });

        await provider.GetRequiredService<IMessagePublisher>().PublishAsync(new OrderCreated(7));

        Assert.Single(collected);
        Assert.Equal(7, collected[0]);
    }

    [Fact]
    public async Task PublishAsync_CorrelationId_IsPassedToMetadata()
    {
        string? capturedCorrelationId = null;

        var handler = Substitute.For<IMessageHandler<OrderCreated>>();
        handler.HandleAsync(Arg.Any<MessageContext<OrderCreated>>(), Arg.Any<CancellationToken>())
               .Returns(ci =>
               {
                   capturedCorrelationId = ci.Arg<MessageContext<OrderCreated>>().Metadata.CorrelationId;
                   return ConsumeResult.Ack;
               });

        var provider = BuildProvider(svc =>
            svc.AddTransient<IMessageHandler<OrderCreated>>(_ => handler));

        await provider.GetRequiredService<IMessagePublisher>()
            .PublishAsync(new OrderCreated(1), new PublishOptions { CorrelationId = "trace-123" });

        Assert.Equal("trace-123", capturedCorrelationId);
    }

    private sealed class CollectingHandler(List<int> collected) : IMessageHandler<OrderCreated>
    {
        public Task<ConsumeResult> HandleAsync(MessageContext<OrderCreated> context, CancellationToken ct)
        {
            collected.Add(context.Message.OrderId);
            return Task.FromResult(ConsumeResult.Ack);
        }
    }
}

public sealed class InMemoryOutboxStoreTests
{
    [Fact]
    public async Task SaveAndGetPending_ReturnsSavedMessage()
    {
        var store = new InMemoryOutboxStore();
        var msg   = new OutboxMessage { MessageType = "OrderCreated", Payload = "{}", Exchange = "orders" };

        await store.SaveAsync(msg);
        var pending = await store.GetPendingAsync(10);

        Assert.Single(pending);
        Assert.Equal(msg.Id, pending[0].Id);
    }

    [Fact]
    public async Task MarkPublished_RemovesFromPending()
    {
        var store = new InMemoryOutboxStore();
        var msg   = new OutboxMessage { MessageType = "OrderCreated", Payload = "{}", Exchange = "orders" };

        await store.SaveAsync(msg);
        await store.MarkPublishedAsync(msg.Id);

        var pending = await store.GetPendingAsync(10);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task MarkFailed_IncreasesAttemptCount()
    {
        var store = new InMemoryOutboxStore();
        var msg   = new OutboxMessage { MessageType = "OrderCreated", Payload = "{}", Exchange = "orders" };

        await store.SaveAsync(msg);
        await store.MarkFailedAsync(msg.Id, "broker unavailable");

        Assert.Equal(1, msg.AttemptCount);
        Assert.Equal("broker unavailable", msg.Error);
    }

    [Fact]
    public async Task GetPending_RespectsBatchSize()
    {
        var store = new InMemoryOutboxStore();
        for (var i = 0; i < 5; i++)
            await store.SaveAsync(new OutboxMessage
            {
                MessageType = "E",
                Payload     = "{}",
                Exchange    = "x",
            });

        var pending = await store.GetPendingAsync(3);
        Assert.Equal(3, pending.Count);
    }
}
