namespace Shotter.Api.Middleware;

using System.Security.Cryptography;
using System.Text;

public sealed class ApiKeyMiddleware(
    RequestDelegate next,
    IConfiguration configuration)
{
    private readonly string? _apiKey =
        configuration["ApiKey"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            await next(context);
            return;
        }

        if (ValidateApiKey(context)) await next(context);
    }

    private bool ValidateApiKey(HttpContext context)
    {
        const string prefix = "Bearer ";

        var authorization =
            context.Request.Headers.Authorization.ToString();

        if (!authorization.StartsWith(
                prefix, StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode =
                StatusCodes.Status401Unauthorized;
            return false;
        }

        var providedKey = authorization[prefix.Length..].Trim();

        var expectedBytes = Encoding.UTF8.GetBytes(_apiKey!);
        var providedBytes = Encoding.UTF8.GetBytes(providedKey);

        if (expectedBytes.Length == providedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(
                expectedBytes, providedBytes)) return true;
        
        context.Response.StatusCode =
            StatusCodes.Status401Unauthorized;
        return false;
    }
}