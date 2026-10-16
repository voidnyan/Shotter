using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shotter.Core.Configuration;
using Shotter.Core.Interfaces;

namespace Shotter.Services.Notifications;

public class DiscordService(IHttpClientFactory httpClientFactory, IOptions<DiscordOptions> options, ILogger<DiscordService> logger) : INotificationService
{
    private readonly DiscordOptions _options = options.Value;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    public async Task SendNotification(string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.Url)) return;

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                _options.Url,
                new { content = message },
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Failed to publish notification to Discord");
            throw;
        }
    }
}