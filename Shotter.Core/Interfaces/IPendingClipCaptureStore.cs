using Shotter.Core.Models;

namespace Shotter.Core.Interfaces;

public interface IPendingClipCaptureStore
{
    PendingClipCapture? Get();
    void Set(PendingClipCapture? capture);
}