using Microsoft.Extensions.DependencyInjection;
using Primitives.Storage.Abstractions;
using Primitives.Storage.Exceptions;
using Primitives.Storage.Extensions;
using Primitives.Storage.Models;

namespace Primitives.Storage.Tests;

/// <summary>Tests for <see cref="Internal.LocalStorageService"/> via the <see cref="IStorageService"/> abstraction.</summary>
public sealed class LocalStorageServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IStorageService _sut;
    private const string Bucket = "test-bucket";

    public LocalStorageServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(),
            "PrimitivesStorageTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        var sp = new ServiceCollection()
            .AddPrimitivesStorage(o => o.BasePath = _tempDir)
            .BuildServiceProvider();

        _sut = sp.GetRequiredService<IStorageService>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── DI registration ──────────────────────────────────────────────────────

    [Fact]
    public void AddPrimitivesStorage_RegistersIStorageService()
    {
        var sp      = new ServiceCollection().AddPrimitivesStorage().BuildServiceProvider();
        var service = sp.GetService<IStorageService>();
        Assert.NotNull(service);
    }

    // ── Upload + Exists ──────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_ThenExists_ReturnsTrue()
    {
        await _sut.UploadAsync(Bucket, "hello.txt", TextStream("hello"));

        var exists = await _sut.ExistsAsync(Bucket, "hello.txt");

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ObjectNotUploaded_ReturnsFalse()
    {
        var exists = await _sut.ExistsAsync(Bucket, "nonexistent.txt");

        Assert.False(exists);
    }

    // ── Round-trip upload + download ─────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_ThenDownload_RoundTripsContent()
    {
        const string content = "Hello, Primitives.Storage!";
        await _sut.UploadAsync(Bucket, "round-trip.txt", TextStream(content));

        await using var stream = await _sut.DownloadAsync(Bucket, "round-trip.txt");
        var downloaded = await new StreamReader(stream).ReadToEndAsync();

        Assert.Equal(content, downloaded);
    }

    [Fact]
    public async Task DownloadAsync_ObjectNotFound_ThrowsStorageException()
    {
        await Assert.ThrowsAsync<StorageException>(() =>
            _sut.DownloadAsync(Bucket, "ghost.txt"));
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingObject_RemovesIt()
    {
        await _sut.UploadAsync(Bucket, "to-delete.txt", TextStream("bye"));
        await _sut.DeleteAsync(Bucket, "to-delete.txt");

        var exists = await _sut.ExistsAsync(Bucket, "to-delete.txt");
        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingObject_DoesNotThrow()
    {
        // Should be a no-op
        var ex = await Record.ExceptionAsync(() => _sut.DeleteAsync(Bucket, "ghost.txt"));
        Assert.Null(ex);
    }

    // ── GetMetadata ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMetadataAsync_AfterUploadWithOptions_ReturnsStorageObject()
    {
        await _sut.UploadAsync(Bucket, "image.png", TextStream("fake-bytes"), new UploadOptions
        {
            ContentType = "image/png",
            Metadata    = new Dictionary<string, string> { ["author"] = "unit-test" },
        });

        var meta = await _sut.GetMetadataAsync(Bucket, "image.png");

        Assert.NotNull(meta);
        Assert.Equal("image.png", meta.Name);
        Assert.Equal("image/png", meta.ContentType);
        Assert.Equal("unit-test", meta.Metadata["author"]);
        Assert.True(meta.SizeBytes > 0);
    }

    [Fact]
    public async Task GetMetadataAsync_ObjectNotFound_ReturnsNull()
    {
        var meta = await _sut.GetMetadataAsync(Bucket, "does-not-exist.bin");
        Assert.Null(meta);
    }

    // ── List ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_WithPrefix_FiltersResults()
    {
        await _sut.UploadAsync(Bucket, "images/cat.jpg",       TextStream("cat"));
        await _sut.UploadAsync(Bucket, "images/dog.jpg",       TextStream("dog"));
        await _sut.UploadAsync(Bucket, "documents/report.pdf", TextStream("pdf"));

        var result = await _sut.ListAsync(Bucket, new ListOptions { Prefix = "images/" });

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, item => Assert.StartsWith("images/", item.Name));
    }

    [Fact]
    public async Task ListAsync_NoPrefix_ReturnsAllObjects()
    {
        await _sut.UploadAsync(Bucket, "a.txt", TextStream("a"));
        await _sut.UploadAsync(Bucket, "b.txt", TextStream("b"));
        await _sut.UploadAsync(Bucket, "c.txt", TextStream("c"));

        var result = await _sut.ListAsync(Bucket);

        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task ListAsync_EmptyBucket_ReturnsEmptyList()
    {
        var result = await _sut.ListAsync("empty-bucket");

        Assert.Empty(result.Items);
        Assert.Null(result.ContinuationToken);
    }

    // ── Signed URL ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSignedUrlAsync_LocalProvider_ThrowsNotSupportedException()
    {
        await Assert.ThrowsAsync<NotSupportedException>(() =>
            _sut.GetSignedUrlAsync(Bucket, "any.txt", new SignedUrlOptions()));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Stream TextStream(string text)
        => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
}
