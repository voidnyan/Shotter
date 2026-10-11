namespace Shotter.Core.Models;

public class CaptureRequest
{
    public bool IncludeSubtitles { get; set; } = false;
    public string? Format { get; set; }
    public int? Height { get; set; }
}