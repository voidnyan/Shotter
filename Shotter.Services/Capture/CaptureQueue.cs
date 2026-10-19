using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Shotter.Core.Configuration;
using Shotter.Core.Interfaces;
using Shotter.Core.Models;

namespace Shotter.Services.Capture;

public sealed class CaptureQueue : ICaptureQueue
{
    private readonly Channel<CaptureJob> _channel;
    
    private int _processing;
    public bool IsProcessing => Volatile.Read(ref _processing) == 1;

    public void SetProcessing(bool processing)
    {
        Interlocked.Exchange(ref _processing, processing ? 1 : 0);
    }
    
    public CaptureQueue(IOptions<ShotterOptions> options)
    {
        _channel = Channel.CreateBounded<CaptureJob>(
            new BoundedChannelOptions(options.Value.ScreenshotQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });
    }
    
    public bool TryEnqueue(CaptureJob job)
    {
        return _channel.Writer.TryWrite(job);
    }

    public ValueTask<CaptureJob> DequeueAsync(
        CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
