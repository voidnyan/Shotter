namespace Shotter.Core.Models;

public class PendingClipCapture
{ 
    public required CurrentPlayback Playback { get; init; }
    public double EndPositionSeconds { get; set; }
    public DateTime CreatedAt { get; set; }
}