using System.Text.Json;
using Microsoft.Extensions.Options;
using Shotter.Core.Configuration;
using Shotter.Core.Exceptions;
using Shotter.Core.Interfaces;
using Shotter.Core.Models;

namespace Shotter.Services.Playback;

public class PlexPlaybackProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<ShotterOptions> options)
    : IPlaybackProvider
{
    private readonly string _plexUrl = options.Value.MediaServerUrl;
    private readonly string _apiKey = options.Value.MediaServerApiKey;
    private readonly string? _userName = options.Value.MediaServerUserId;

    private readonly HttpClient _httpClient =
        httpClientFactory.CreateClient();

    public async Task<CurrentPlayback> GetPlaybackInformation(
        CancellationToken cancellationToken)
    {
        var responseData = await GetPlexJsonAsync<PlexSessionsResponse>(
            "/status/sessions",
            cancellationToken);

        var session = responseData?.MediaContainer?.Metadata?
            .FirstOrDefault(x => string.IsNullOrEmpty(_userName) || string.Equals(
                x.User?.Title,
                _userName,
                StringComparison.OrdinalIgnoreCase));

        if (session == null)
        {
            throw new PlaybackProviderException(
                "The configured Plex server is not currently playing anything.");
        }

        var media = session.Media?.FirstOrDefault();

        if (media == null)
        {
            throw new PlaybackProviderException(
                "Plex returned no media information for the current playback.");
        }

        var part = media.Part?.FirstOrDefault();
        var filePath = part?.File;

        if (string.IsNullOrEmpty(filePath))
        {
            filePath = await ResolveFilePathFromMetadataAsync(
                session.RatingKey,
                part?.Id ?? default,
                cancellationToken);
        }

        if (string.IsNullOrEmpty(filePath))
        {
            throw new PlaybackProviderException(
                "Could not determine the media file path.");
        }

        var positionSeconds = session.ViewOffset / 1000.0;

        var subtitles = GetSelectedSubtitles(part);

        return new CurrentPlayback
        {
            MediaPath = filePath,
            PositionSeconds = positionSeconds,

            IndexNumber = session.Index,
            ParentIndexNumber = session.ParentIndex,
            SeriesName = session.GrandparentTitle,

            IsMovie = string.Equals(
                session.Type,
                "movie",
                StringComparison.OrdinalIgnoreCase),

            Name = session.Title,

            SubtitlesCodec = subtitles.codec,
            SubtitlesIndex = subtitles.subtitleIndex,
            ExternalSubtitlePath = subtitles.subtitlePath
        };
    }

    private async Task<string?> ResolveFilePathFromMetadataAsync(
        string? ratingKey,
        JsonElement sessionPartId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(ratingKey))
        {
            return null;
        }

        PlexSessionsResponse? metadataResponse;

        try
        {
            metadataResponse = await GetPlexJsonAsync<PlexSessionsResponse>(
                $"/library/metadata/{ratingKey}",
                cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new PlaybackProviderException(
                $"Failed to fetch library metadata for ratingKey '{ratingKey}'. {exception.Message}");
        }

        var parts = metadataResponse?.MediaContainer?.Metadata?
            .FirstOrDefault()
            ?.Media?
            .SelectMany(m => m.Part ?? Enumerable.Empty<PlexPart>())
            .ToList();

        if (parts == null || parts.Count == 0)
        {
            return null;
        }

        var sessionPartIdText = FormatPlexId(sessionPartId);
        if (sessionPartIdText != null)
        {
            var matched = parts.FirstOrDefault(p =>
                FormatPlexId(p.Id) == sessionPartIdText && !string.IsNullOrEmpty(p.File));
            if (matched?.File != null)
            {
                return matched.File;
            }
        }

        return parts.FirstOrDefault(p => !string.IsNullOrEmpty(p.File))?.File;
    }

    // Plex emits ids as JSON numbers or strings depending on endpoint/client.
    private static string? FormatPlexId(JsonElement id) =>
        id.ValueKind switch
        {
            JsonValueKind.Number => id.GetRawText(),
            JsonValueKind.String => id.GetString(),
            _ => null
        };

    private async Task<T?> GetPlexJsonAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_plexUrl}{path}");

        request.Headers.TryAddWithoutValidation(
            "X-Plex-Token",
            _apiKey);

        request.Headers.TryAddWithoutValidation(
            "X-Plex-Client-Identifier",
            "Shotter");

        request.Headers.TryAddWithoutValidation(
            "X-Plex-Product",
            "Shotter");

        request.Headers.TryAddWithoutValidation(
            "Accept",
            "application/json");

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var responseStream =
            await response.Content.ReadAsStreamAsync(cancellationToken);

        return await JsonSerializer.DeserializeAsync<T>(
            responseStream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            },
            cancellationToken);
    }

    private static (
        string? codec,
        int? subtitleIndex,
        string? subtitlePath)
        GetSelectedSubtitles(PlexPart? part)
    {
        if (part?.Stream == null)
        {
            return (null, null, null);
        }

        var subtitleStreams = part.Stream
            .Where(x => x.StreamType == 3)
            .ToList();

        var selectedSubtitle = subtitleStreams
            .FirstOrDefault(x => x.Selected);

        if (selectedSubtitle == null)
        {
            return (null, null, null);
        }

        var subtitleIndex = subtitleStreams.IndexOf(selectedSubtitle);

        return (
                selectedSubtitle.Codec,
                subtitleIndex,
                selectedSubtitle.External
                    ? selectedSubtitle.Path
                    : null);
    }


    private sealed class PlexSessionsResponse
    {
        public PlexMediaContainer? MediaContainer { get; set; }
    }

    private sealed class PlexMediaContainer
    {
        public List<PlexMetadata>? Metadata { get; set; }
    }

    private sealed class PlexMetadata
    {
        public string? RatingKey { get; set; }
        public string? Type { get; set; }
        public string? Title { get; set; }
        public string? GrandparentTitle { get; set; }

        public int? Index { get; set; }
        public int? ParentIndex { get; set; }

        public long ViewOffset { get; set; }

        public PlexUser? User { get; set; }
        public PlexPlayer? Player { get; set; }
        public PlexSession? Session { get; set; }

        public List<PlexMedia>? Media { get; set; }
    }

    private sealed class PlexUser
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
    }

    private sealed class PlexPlayer
    {
        // Plex emits userID as a JSON number; System.Text.Json will not coerce Number to string.
        public JsonElement UserID { get; set; }
        public string? State { get; set; }
        public string? Product { get; set; }
        public string? Title { get; set; }
    }

    private sealed class PlexSession
    {
        public string? Id { get; set; }
    }

    private sealed class PlexMedia
    {
        public List<PlexPart>? Part { get; set; }
    }

    private sealed class PlexPart
    {
        // Plex may emit Part.id as a JSON number or string.
        public JsonElement Id { get; set; }

        public string? File { get; set; }

        public List<PlexStream>? Stream { get; set; }
    }

    private sealed class PlexStream
    {
        public int StreamType { get; set; }

        public string? Codec { get; set; }

        public bool Selected { get; set; }

        public bool External { get; set; }

        public string? Path { get; set; }
    }
}
