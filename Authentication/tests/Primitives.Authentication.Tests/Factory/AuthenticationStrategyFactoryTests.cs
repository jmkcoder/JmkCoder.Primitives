using NSubstitute;
using Primitives.Authentication.Abstractions;
using Primitives.Authentication.Exceptions;
using Primitives.Authentication.Factory;

namespace Primitives.Authentication.Tests.Factory;

public class AuthenticationStrategyFactoryTests
{
    private static IAuthenticationStrategy MakeStrategy(string name)
    {
        var s = Substitute.For<IAuthenticationStrategy>();
        s.Name.Returns(name);
        return s;
    }

    [Fact]
    public void GetStrategy_ReturnsCorrectStrategy_CaseInsensitive()
    {
        var alpha   = MakeStrategy("Alpha");
        var factory = new AuthenticationStrategyFactory([alpha]);

        Assert.Same(alpha, factory.GetStrategy("alpha"));
        Assert.Same(alpha, factory.GetStrategy("ALPHA"));
        Assert.Same(alpha, factory.GetStrategy("Alpha"));
    }

    [Fact]
    public void GetStrategy_ThrowsAuthenticationException_WhenNotFound()
    {
        var factory = new AuthenticationStrategyFactory([MakeStrategy("Existing")]);

        var ex = Assert.Throws<AuthenticationException>(() => factory.GetStrategy("Missing"));
        Assert.Equal("Missing", ex.StrategyName);
        Assert.Equal(AuthenticationFailureReason.StrategyNotFound, ex.Reason);
    }

    [Fact]
    public void TryGetStrategy_ReturnsFalse_WhenNotFound()
    {
        var factory = new AuthenticationStrategyFactory([MakeStrategy("A")]);
        Assert.False(factory.TryGetStrategy("B", out var strategy));
        Assert.Null(strategy);
    }

    [Fact]
    public void TryGetStrategy_ReturnsTrue_WhenFound()
    {
        var alpha   = MakeStrategy("Alpha");
        var factory = new AuthenticationStrategyFactory([alpha]);
        Assert.True(factory.TryGetStrategy("alpha", out var found));
        Assert.Same(alpha, found);
    }

    [Fact]
    public void RegisteredStrategyNames_ContainsAllNames()
    {
        var factory = new AuthenticationStrategyFactory(
            [MakeStrategy("A"), MakeStrategy("B"), MakeStrategy("C")]);

        Assert.Contains("A", factory.RegisteredStrategyNames);
        Assert.Contains("B", factory.RegisteredStrategyNames);
        Assert.Contains("C", factory.RegisteredStrategyNames);
    }

    [Fact]
    public void GetStrategy_ThrowsArgumentException_ForNullOrWhitespace()
    {
        var factory = new AuthenticationStrategyFactory([MakeStrategy("A")]);
        Assert.Throws<ArgumentException>(() => factory.GetStrategy(""));
        Assert.Throws<ArgumentException>(() => factory.GetStrategy("   "));
    }
}
