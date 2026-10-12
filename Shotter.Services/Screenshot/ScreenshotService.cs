using Microsoft.Extensions.Logging;
using Shotter.Core.Enums;
using Shotter.Core.Exceptions;
using Shotter.Core.Interfaces;
using Shotter.Core.Models;

namespace Shotter.Services.Screenshot;

public partial class ScreenshotService(
    IPlaybackProvider playbackProvider, 
    ILogger<ScreenshotService> logger,
    ICaptureQueue captureQueue,
    IFileNameResolver fileNameResolver) : IScreenshotService
{
    public async Task HandleScreenshotRequest(
        CaptureRequest request,
        CancellationToken cancellationToken)
    {
        var mediaInfo= await playbackProvider.GetPlaybackInformation(cancellationToken);
        var output = fileNameResolver.ResolveOutputPath(mediaInfo);
        
        var job = new CaptureJob(
            CaptureType.Screenshot,
            mediaInfo.MediaPath,
            mediaInfo.PositionSeconds,
            request,
            mediaInfo.SubtitlesIndex,
            mediaInfo.SubtitlesCodec,
            mediaInfo.ExternalSubtitlePath,
            Path.Combine(output.outputDirectory, output.outputFile),
            output.outputDirectory);
        
        LogQueryingJob(job.MediaPath, job.PositionSeconds, request.IncludeSubtitles);
        
        if (captureQueue.TryEnqueue(job))
        {
            return;
        }
        
        throw new CaptureQueueFullException();
    }

    [LoggerMessage(LogLevel.Information, "Queuing screenshot job for {MediaPath} at {PositionSeconds}s with subtitles {IncludeSubtitles}")]
    partial void LogQueryingJob(string mediaPath, double positionSeconds, bool includeSubtitles);
}