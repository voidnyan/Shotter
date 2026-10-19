using System.Diagnostics;
using Shotter.Core.Models;

namespace Shotter.Services.Capture;

public abstract class BaseProcessor
{
    
    protected static async Task RunProcess(ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
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
    
    protected async Task<int?> GetResolutionHeight(CaptureRequest request, string mediaPath, int? optionsHeight)
    {
        if (request.Height < 1) return null;
        
        var height = request.Height ?? optionsHeight;

        if (height is null or < 1) return null;

        var sourceHeight = await GetVideoHeightAsync(mediaPath);
        height = Math.Min(sourceHeight, height.Value);

        return height == sourceHeight ? null : height;
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
}