using Shotter.Core.Enums;
using Shotter.Core.Exceptions;
using Shotter.Core.Interfaces;
using Shotter.Core.Models;

namespace Shotter.Services.Capture;

public class ClipService(
    IPlaybackProvider playbackProvider, 
    IPendingClipCaptureStore pendingClipCaptureStore,
    ICaptureQueue captureQueue,
    IFileNameResolver fileNameResolver) : IClipService
{
    public async Task ProcessClipRequestAsync(CaptureRequest request, CancellationToken cancellationToken)
    {
        var pendingCapture = pendingClipCaptureStore.Get();
        var currentPlayback = await playbackProvider.GetPlaybackInformation(cancellationToken);
        if (IsFirstCall(pendingCapture, currentPlayback))
        {
            pendingCapture = new PendingClipCapture
            {
                Playback = currentPlayback,
                CreatedAt = DateTime.UtcNow
            };
            pendingClipCaptureStore.Set(pendingCapture);
            return;
        }

        var originalPlayback = pendingCapture!.Playback;
        var output = fileNameResolver.ResolveOutputPath(currentPlayback);

        var job = new CaptureJob(
            CaptureType.Gif,
            currentPlayback.MediaPath,
            originalPlayback.PositionSeconds,
            request,
            currentPlayback.SubtitlesIndex,
            currentPlayback.SubtitlesCodec,
            currentPlayback.ExternalSubtitlePath,
            Path.Combine(output.outputDirectory, output.outputFile),
            output.outputDirectory,
            currentPlayback.PositionSeconds);
        
        pendingClipCaptureStore.Set(null);
        
        if (!captureQueue.TryEnqueue(job))
        {
            throw new CaptureQueueFullException();
        }
    }

    private bool IsFirstCall(PendingClipCapture? pendingClipCapture, CurrentPlayback currentPlayback)
    {
        if (pendingClipCapture == null) return true;

        pendingClipCaptureStore.Set(null);
        
        if (DateTimeOffset.UtcNow - pendingClipCapture.CreatedAt >= TimeSpan.FromMinutes(1) ||
            currentPlayback.PositionSeconds < pendingClipCapture.Playback.PositionSeconds)
        {
            return true;
        };

        return false;
    }
}