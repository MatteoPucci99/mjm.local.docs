using Microsoft.Extensions.Options;
using Mjm.LocalDocs.Core.Models;
using Mjm.LocalDocs.Core.Services;

namespace Mjm.LocalDocs.Server.Middleware;

/// <summary>
/// Middleware that validates Bearer tokens for MCP endpoint requests.
/// </summary>
public sealed class McpAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<McpAuthenticationMiddleware> _logger;
    private readonly McpOptions _options;

    public McpAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<McpAuthenticationMiddleware> logger,
        IOptions<McpOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, ApiTokenService tokenService)
    {
        // Skip authentication if not required
        if (!_options.RequireAuthentication)
        {
            _logger.LogDebug("MCP authentication is disabled, allowing request");
            await _next(context);
            return;
        }

        // Extract the Authorization header
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        
        if (string.IsNullOrEmpty(authHeader))
        {
            _logger.LogWarning("MCP request without Authorization header from {RemoteIp}",
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await context.Response.WriteAsJsonAsync(new { error = "Authorization header is required" });
            return;
        }

        // Validate Bearer token format
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("MCP request with invalid Authorization format from {RemoteIp}",
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await context.Response.WriteAsJsonAsync(new { error = "Invalid authorization format. Expected: Bearer <token>" });
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("MCP request with empty Bearer token from {RemoteIp}",
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await context.Response.WriteAsJsonAsync(new { error = "Token is required" });
            return;
        }

        // Validate the token
        var validatedToken = await tokenService.ValidateTokenAsync(token, updateLastUsed: true);

        if (validatedToken == null)
        {
            _logger.LogWarning("MCP request with invalid or expired token from {RemoteIp}",
                context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer error=\"invalid_token\"";
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired token" });
            return;
        }

        _logger.LogDebug("MCP request authenticated with token '{TokenName}' (prefix: {TokenPrefix})",
            validatedToken.Name, validatedToken.TokenPrefix);

        // Store token info in HttpContext for potential use by downstream components
        context.Items["McpToken"] = validatedToken;

        await _next(context);
    }
}

/// <summary>
/// Extension methods for McpAuthenticationMiddleware.
/// </summary>
public static class McpAuthenticationMiddlewareExtensions
{
    /// <summary>
    /// Adds MCP authentication middleware for the specified path.
    /// </summary>
    public static IApplicationBuilder UseMcpAuthentication(this IApplicationBuilder app, string path = "/mcp")
    {
        return app.UseWhen(
            context => context.Request.Path.StartsWithSegments(path),
            appBuilder => appBuilder.UseMiddleware<McpAuthenticationMiddleware>());
    }
}
