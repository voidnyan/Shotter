using Shotter.Core.Models;

namespace Shotter.Core.Interfaces;

public interface IScreenshotService
{
    Task HandleScreenshotRequest(CaptureRequest request,
        CancellationToken cancellationToken);
}