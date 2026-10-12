using Shotter.Core.Models;

namespace Shotter.Core.Interfaces;

public interface IClipService
{
    Task ProcessClipRequestAsync(CancellationToken cancellationToken);
}