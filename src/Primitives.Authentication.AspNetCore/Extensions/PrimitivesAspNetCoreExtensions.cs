using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Primitives.Authentication.AspNetCore.Grpc;
using Primitives.Authentication.AspNetCore.SignalR;
using Primitives.Authentication.Strategies.TokenIssuance;
using System.Text;

namespace Primitives.Authentication.AspNetCore.Extensions;

public static class PrimitivesAspNetCoreExtensions
{
    /// <summary>
    /// Adds JWT Bearer authentication pre-configured to validate tokens produced by
    /// <c>JwtTokenService</c> (HS256, same issuer/audience/key as <see cref="JwtOptions"/>).
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddAuthentication()
    ///         .AddPrimitivesJwtBearer(o =>
    ///         {
    ///             o.Issuer     = "https://auth.example.com";
    ///             o.Audience   = "https://api.example.com";
    ///             o.SigningKey  = configuration["Jwt:SigningKey"]!;
    ///         });
    /// </code>
    /// </example>
    public static AuthenticationBuilder AddPrimitivesJwtBearer(
        this AuthenticationBuilder builder,
        Action<JwtOptions>         configure,
        string                     scheme = JwtBearerDefaults.AuthenticationScheme)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var opts = new JwtOptions();
        configure(opts);

        return builder.AddJwtBearer(scheme, o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidIssuer              = opts.Issuer,
                ValidateAudience         = true,
                ValidAudience            = opts.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = new SymmetricSecurityKey(
                                               Encoding.UTF8.GetBytes(opts.SigningKey)),
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.FromSeconds(30),
            };

            // Allow SignalR / WebSocket clients to pass token via query string
            o.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var token = ctx.Request.Query["access_token"].ToString();
                    var path  = ctx.HttpContext.Request.Path;

                    // Only apply query-string token for hub paths
                    if (!string.IsNullOrEmpty(token) &&
                        path.StartsWithSegments("/hubs", System.StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Token = token;
                    }

                    return Task.CompletedTask;
                },
            };
        });
    }

    /// <summary>
    /// Registers the gRPC <see cref="AuthenticationServerInterceptor"/> and the SignalR
    /// <see cref="AuthenticationHubFilter"/> as singletons.
    /// </summary>
    /// <remarks>
    /// <strong>Prerequisite:</strong> call
    /// <c>services.AddAuthentication().AddJwtTokenIssuance(...)</c> from
    /// <c>Primitives.Authentication</c> before this method so that
    /// <see cref="Primitives.Authentication.Strategies.TokenIssuance.IJwtTokenValidator"/>
    /// is registered in DI.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown at first resolution time if <c>IJwtTokenValidator</c> is not registered.
    /// </exception>
    public static IServiceCollection AddPrimitivesAspNetCoreAuthentication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Use factory registration so the missing-dependency error message is actionable.
        services.AddSingleton<AuthenticationServerInterceptor>(sp =>
        {
            var validator = sp.GetService<IJwtTokenValidator>()
                ?? throw new InvalidOperationException(
                    "IJwtTokenValidator is not registered. " +
                    "Call services.AddAuthentication().AddJwtTokenIssuance(o => { ... }) " +
                    "from Primitives.Authentication before calling AddPrimitivesAspNetCoreAuthentication().");

            var logger = sp.GetRequiredService<ILogger<AuthenticationServerInterceptor>>();
            return new AuthenticationServerInterceptor(validator, logger);
        });

        services.AddSingleton<AuthenticationHubFilter>(sp =>
        {
            var validator = sp.GetService<IJwtTokenValidator>()
                ?? throw new InvalidOperationException(
                    "IJwtTokenValidator is not registered. " +
                    "Call services.AddAuthentication().AddJwtTokenIssuance(o => { ... }) " +
                    "from Primitives.Authentication before calling AddPrimitivesAspNetCoreAuthentication().");

            var logger = sp.GetRequiredService<ILogger<AuthenticationHubFilter>>();
            return new AuthenticationHubFilter(validator, logger);
        });

        return services;
    }
}
