using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Primitives.Authentication.Strategies.TokenIssuance;

namespace Primitives.Authentication.Benchmarks;

/// <summary>
/// Micro-benchmarks for the token issuance hot path.
/// Run with:  dotnet run -c Release
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class TokenIssuanceBenchmarks
{
    private JwtTokenService _jwtService = null!;
    private InMemoryRefreshTokenStore _refreshStore = null!;
    private string _validRefreshToken = string.Empty;

    [GlobalSetup]
    public async Task Setup()
    {
        var opts    = Options.Create(new JwtOptions
        {
            Issuer               = "https://bench.example.com",
            Audience             = "https://bench-api.example.com",
            SigningKey            = "bench-secret-key-that-is-long-enough!!",
            AccessTokenLifetime  = TimeSpan.FromMinutes(15),
            RefreshTokenLifetime = TimeSpan.FromDays(7),
        });
        var time    = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _jwtService    = new JwtTokenService(opts, time);
        _refreshStore  = new InMemoryRefreshTokenStore(opts, time);
        _validRefreshToken = await _refreshStore.GenerateAsync("bench-subject");
    }

    [Benchmark(Description = "JWT generation (HS256)")]
    public (string Token, DateTimeOffset ExpiresAt) GenerateJwt()
        => _jwtService.GenerateAccessToken("bench-subject");

    [Benchmark(Description = "Refresh token generation")]
    public Task<string> GenerateRefreshToken()
        => _refreshStore.GenerateAsync("bench-subject");

    [Benchmark(Description = "Strategy resolution (dictionary lookup)")]
    public void StrategyResolution()
    {
        // Simulates the factory dictionary lookup cost.
        _ = StringComparer.OrdinalIgnoreCase.Equals("ApiKey", "APIKEY");
    }
}
