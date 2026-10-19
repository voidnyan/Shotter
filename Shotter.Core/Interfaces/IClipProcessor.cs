using Shotter.Core.Models;

namespace Shotter.Core.Interfaces;

public interface IClipProcessor
{
    Task TakeGif(
        CancellationToken cancellationToken,
        double positionSeconds, 
        double endPositionSeconds,
        string mediaPath,
        string outputPath,
        ClipCaptureRequest request);
}