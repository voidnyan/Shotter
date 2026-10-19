using Shotter.Core.Models;

namespace Shotter.Core.Interfaces;

public interface IClipService
{
    Task ProcessClipRequestAsync(CaptureRequest request, CancellationToken cancellationToken);
}