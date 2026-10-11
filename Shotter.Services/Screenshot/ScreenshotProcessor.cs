using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Options;
using Shotter.Core.Configuration;
using Shotter.Core.Interfaces;
using Shotter.Core.Models;

namespace Shotter.Services.Screenshot;

public class ScreenshotProcessor(IOptions<FfmpegOptions> options) : IScreenshotProcessor
{
    private const int PreSeekInSeconds = 5;
    private readonly FfmpegOptions _options = options.Value;
    
    public async Task TakeScreenshot(
        CancellationToken cancellationToken,
        double positionSeconds,
        string mediaPath,
        string outputPath,
        CaptureRequest request,
        int? subtitlesIndex,
        string? subtitlesCodec,
        string? externalSubtitlePath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (request.IncludeSubtitles)
        {
            await WithSubs(
                startInfo, 
                request, 
                positionSeconds,
                mediaPath, 
                subtitlesIndex, 
                subtitlesCodec, 
                externalSubtitlePath);
        }
        else
        {
            await WithoutSubs(
                startInfo,
                request,
                positionSeconds,
                mediaPath);
        }

        startInfo.ArgumentList.Add("-frames:v");
        startInfo.ArgumentList.Add("1");

        SetQuality(startInfo, request);

        startInfo.ArgumentList.Add("-y");
        startInfo.ArgumentList.Add(outputPath + "." + GetFileExtension(request));

        using var process = new Process();
        process.StartInfo = startInfo;

        process.Start();

        var stderrTask = process.StandardError.ReadToEndAsync(
            cancellationToken);

        var stdoutTask = process.StandardOutput.ReadToEndAsync(
            cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var stderr = await stderrTask;
        _ = await stdoutTask;
        
        if (process.ExitCode != 0)
        {
            throw new Exception(stderr);
        }
    }

    private async Task WithSubs(
        ProcessStartInfo startInfo,
        CaptureRequest request,
        double positionSeconds,
        string mediaPath,
        int? subtitlesIndex,
        string? subtitlesCodec,
        string? externalSubtitlePath)
    {
        switch (subtitlesCodec?.ToLowerInvariant())
        {
            case "ssa":
            case "ass":
            case "subrip": // SRT
                await HandleAsslibSubs(
                    startInfo, 
                    request,
                    positionSeconds, 
                    mediaPath, 
                    subtitlesIndex, 
                    externalSubtitlePath);
                break;
            case "pgssub":
            case "pgs":
            case "vobsub":
                await HandlePgsSubs(
                    startInfo, 
                    request,
                    positionSeconds, 
                    mediaPath, 
                    subtitlesIndex);
                break;
            default:
                await WithoutSubs(
                    startInfo,
                    request, 
                    positionSeconds, 
                    mediaPath);
                break;
        }
    }
    
    private async Task HandlePgsSubs(
        ProcessStartInfo startInfo,
        CaptureRequest request,
        double positionSeconds, 
        string mediaPath,
        int? subtitlesIndex)
    {
        // Seeking directy to the target timestamp resulted in the subtitles not being rendered.
        // Seek a few seconds before the target first.
        var seekPosition = Math.Max(0, positionSeconds - PreSeekInSeconds);

        startInfo.ArgumentList.Add("-ss");
        startInfo.ArgumentList.Add(
            seekPosition.ToString(CultureInfo.InvariantCulture));

        startInfo.ArgumentList.Add("-copyts");

        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(mediaPath);

        var filter =
            $"[0:v:0][0:s:{subtitlesIndex}]overlay[overlaid];" +
            $"[overlaid]select='gte(t\\,{positionSeconds.ToString(CultureInfo.InvariantCulture)})'[out]";
        var resolutionFilter = await GetResolutionFilter(request, mediaPath);

        if (!string.IsNullOrEmpty(resolutionFilter)) 
            filter += resolutionFilter;
            
        startInfo.ArgumentList.Add("-filter_complex");
        startInfo.ArgumentList.Add(filter);

        startInfo.ArgumentList.Add("-map");
        startInfo.ArgumentList.Add("[out]");
    }

    private async Task HandleAsslibSubs(
        ProcessStartInfo startInfo,
        CaptureRequest request,
        double positionSeconds,
        string mediaPath,
        int? subtitlesIndex,
        string? externalSubtitlePath)
    {
        startInfo.ArgumentList.Add("-ss");
        startInfo.ArgumentList.Add(positionSeconds.ToString(CultureInfo.InvariantCulture));

        startInfo.ArgumentList.Add("-copyts");
        
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(mediaPath);
        
        var filter = string.IsNullOrEmpty(externalSubtitlePath)
            ? $"subtitles=filename='{mediaPath}':si={subtitlesIndex}"
            : $"subtitles=filename='{externalSubtitlePath}'";
        
        var resolutionFilter = await GetResolutionFilter(request, mediaPath);
        if (!string.IsNullOrEmpty(resolutionFilter))
            filter += "," + resolutionFilter;
        
        startInfo.ArgumentList.Add("-vf");

        startInfo.ArgumentList.Add(filter);
    }

    private async Task WithoutSubs(
        ProcessStartInfo startInfo, 
        CaptureRequest request,
        double positionSeconds, 
        string mediaPath)
    {
        startInfo.ArgumentList.Add("-ss");
        startInfo.ArgumentList.Add(
            positionSeconds.ToString(
                CultureInfo.InvariantCulture));

        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(mediaPath);

        var resolutionFilter = await GetResolutionFilter(request, mediaPath);
        if (string.IsNullOrEmpty(resolutionFilter)) return;
        
        startInfo.ArgumentList.Add("-vf");
        startInfo.ArgumentList.Add(resolutionFilter);
    }

    private async Task<string> GetResolutionFilter(CaptureRequest request, string mediaPath)
    {
        var height = request.Height ?? _options.ScreenshotHeight;
        if (!height.HasValue) return string.Empty;

        var sourceHeight = await GetVideoHeightAsync(mediaPath);
        height = Math.Min(sourceHeight, height.Value);
        
        return $"scale=-2:{height}:flags=lanczos";
    }
    
    private static async Task<int> GetVideoHeightAsync(
        string mediaPath,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffprobe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("error");
        startInfo.ArgumentList.Add("-select_streams");
        startInfo.ArgumentList.Add("v:0");
        startInfo.ArgumentList.Add("-show_entries");
        startInfo.ArgumentList.Add("stream=height");
        startInfo.ArgumentList.Add("-of");
        startInfo.ArgumentList.Add("default=noprint_wrappers=1:nokey=1");
        startInfo.ArgumentList.Add(mediaPath);

        using var process = Process.Start(startInfo)
                            ?? throw new InvalidOperationException("Failed to start ffprobe.");

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0 ||
            !int.TryParse(output.Trim(), out var height))
        {
            throw new InvalidOperationException(
                $"Could not determine video height. {error}");
        }

        return height;
    }

    private string GetFileExtension(CaptureRequest request)
    {
        return request.Format ?? _options.ScreenshotFileExtension;
    }

    private void SetQuality(ProcessStartInfo startInfo, CaptureRequest request)
    {
        switch (request.Format ?? _options.ScreenshotFileExtension.ToLowerInvariant())
        {
            case "jpg":
            case "jpeg":
                startInfo.ArgumentList.Add("-q:v");
                startInfo.ArgumentList.Add(_options.JpgQuality);
                break;

            case "webp":
                startInfo.ArgumentList.Add("-quality");
                startInfo.ArgumentList.Add(_options.WebpQuality);
                break;

            case "png":
                startInfo.ArgumentList.Add("-compression_level");
                startInfo.ArgumentList.Add(_options.PngQuality);
                break;
        }
    }
}