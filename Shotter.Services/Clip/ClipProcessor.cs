using Shotter.Core.Interfaces;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Options;
using Shotter.Core.Configuration;
using Shotter.Core.Models;

namespace Shotter.Services.Clip;

public class ClipProcessor(IOptions<FfmpegOptions> options) : IClipProcessor
{
    private readonly FfmpegOptions _options = options.Value; 
    
    public async Task TakeGif(
        CancellationToken cancellationToken,
        double positionSeconds, 
        double endPositionSeconds,
        string mediaPath,
        string outputPath)
    {
        var duration = endPositionSeconds - positionSeconds;
        duration = Math.Min(duration, _options.GifMaxLength);

        if (duration <= 0)
            throw new ArgumentException(
                "End position must be greater than start position.");
        
        using var process = new Process();

        process.StartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.StartInfo.ArgumentList.Add("-ss");
        process.StartInfo.ArgumentList.Add(
            positionSeconds.ToString(CultureInfo.InvariantCulture));
        
        process.StartInfo.ArgumentList.Add("-t");
        process.StartInfo.ArgumentList.Add(
            duration.ToString(CultureInfo.InvariantCulture));

        process.StartInfo.ArgumentList.Add("-i");
        process.StartInfo.ArgumentList.Add(mediaPath);

        const string optimizationFilters =
            ",split[s0][s1];[s0]palettegen=max_colors=128:stats_mode=diff[p];[s1][p]paletteuse=dither=bayer:bayer_scale=5:diff_mode=rectangle";
        
        process.StartInfo.ArgumentList.Add("-vf");
        process.StartInfo.ArgumentList.Add(
            $"fps={_options.GifFps},scale=-2:{_options.GifHeight}:flags=lanczos{optimizationFilters}");
        
        process.StartInfo.ArgumentList.Add(outputPath + ".gif");

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
}