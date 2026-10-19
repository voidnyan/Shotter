using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Options;
using Shotter.Core.Configuration;
using Shotter.Core.Interfaces;
using Shotter.Core.Models;

namespace Shotter.Services.Capture;

public class ClipProcessor(IOptions<FfmpegOptions> options) : BaseProcessor, IClipProcessor
{
    private readonly FfmpegOptions _options = options.Value; 
    
    public async Task TakeGif(
        CancellationToken cancellationToken,
        double positionSeconds, 
        double endPositionSeconds,
        string mediaPath,
        string outputPath,
        ClipCaptureRequest request)
    {
        var duration = GetDuration(positionSeconds, endPositionSeconds);
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-ss");
        startInfo.ArgumentList.Add(
            positionSeconds.ToString(CultureInfo.InvariantCulture));
        
        startInfo.ArgumentList.Add("-t");
        startInfo.ArgumentList.Add(
            duration.ToString(CultureInfo.InvariantCulture));

        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(mediaPath);

        const string optimizationFilters =
            ",split[s0][s1];[s0]palettegen=max_colors=128:stats_mode=diff[p];[s1][p]paletteuse=dither=bayer:bayer_scale=5:diff_mode=rectangle";
        
        var height = await GetResolutionHeight(request, mediaPath, _options.GifHeight);
        var fps = GetFps(request);
        
        startInfo.ArgumentList.Add("-vf");
        startInfo.ArgumentList.Add(
            $"fps={fps},scale=-2:{height}:flags=lanczos{optimizationFilters}");
        
        startInfo.ArgumentList.Add(outputPath + ".gif");

        await RunProcess(startInfo, cancellationToken);
    }

    private double GetDuration(double positionSeconds, double endPositionSeconds)
    {
        var duration = endPositionSeconds - positionSeconds;
        duration = Math.Min(duration, _options.GifMaxLength);

        if (duration <= 0)
            throw new ArgumentException(
                "End position must be greater than start position.");
        return duration;
    }

    private int GetFps(ClipCaptureRequest request)
    {
        // TODO: Get Video fps as max fps
        var fps = request.Fps ?? _options.GifFps;
        return Math.Min(fps, 24);
    }
}