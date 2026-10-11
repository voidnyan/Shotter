using Shotter.Core.Models;

namespace Shotter.Core.Interfaces;

public interface IScreenshotProcessor
{
    Task TakeScreenshot(CancellationToken cancellationToken, double positionSeconds,
        string mediaPath,
        string outputPath,
        CaptureRequest request,
        int? subtitlesIndex,
        string? subtitlesCodec,
        string? externalSubtitlePath);
}