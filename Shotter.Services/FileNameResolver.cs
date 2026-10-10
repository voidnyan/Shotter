using Shotter.Core.Interfaces;
using Shotter.Core.Models;

namespace Shotter.Services;

public class FileNameResolver : IFileNameResolver
{
    private const string ScreenshotDirectory = "/screenshots";
    private const string ShowsDirectory = "shows";
    private const string MoviesDirectory = "movies";
    
    public (string outputDirectory, string outputFile) ResolveOutputPath(CurrentPlayback mediaInfo)
    {
        var videoTimeStamp = GetVideoTimestamp(mediaInfo.PositionSeconds);
        
        if (mediaInfo.IsMovie)
        {
            var movieNamePath = SanitizePathComponent(mediaInfo.Name!.Replace(" ", "_"));
            var movieDirectory = Path.Combine(ScreenshotDirectory, MoviesDirectory, movieNamePath);
            Directory.CreateDirectory(movieDirectory);
            return (movieDirectory, 
                $"{SanitizePathComponent(movieNamePath)}_{videoTimeStamp}");
        }
        var seriesNamePath = SanitizePathComponent(mediaInfo.SeriesName!.Replace(" ", "_"));
        var seriesDirectory = Path.Combine(ScreenshotDirectory, ShowsDirectory, seriesNamePath);
        
        Directory.CreateDirectory(seriesDirectory);

        return (seriesDirectory,
            $"{SanitizePathComponent(seriesNamePath)}_S{mediaInfo.ParentIndexNumber:D2}E{mediaInfo.IndexNumber:D2}_{videoTimeStamp}");
    }

    private static string SanitizePathComponent(string path)
    {
        path = path.Replace(" ", "_");
        
        var invalidChars = Path.GetInvalidFileNameChars()
            .Concat("<>:\"/\\|?*".ToCharArray())
            .Distinct()
            .ToHashSet();

        return new string(path
                .Where(c => !invalidChars.Contains(c))
                .ToArray())
            .Trim()
            .TrimEnd('.');
    }

    private static string GetVideoTimestamp(double positionSeconds)
    {
        var time = TimeSpan.FromSeconds(positionSeconds);

        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours:D2}h{time.Minutes:D2}m{time.Seconds:D2}s{time.Milliseconds:D3}ms"
            : $"{time.Minutes:D2}m{time.Seconds:D2}s{time.Milliseconds:D3}ms";
    }
}