using Shotter.Core.Interfaces;
using Shotter.Core.Models;

namespace Shotter.Services.Clip;

public sealed class PendingClipCaptureStore : IPendingClipCaptureStore
{
    private PendingClipCapture? _capture;

    public PendingClipCapture? Get() => Volatile.Read(ref _capture);

    public void Set(PendingClipCapture? capture) => Interlocked.Exchange(ref _capture, capture);
}