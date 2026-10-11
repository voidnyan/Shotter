namespace Shotter.Core.Configuration;

public class FfmpegOptions
{
    public string ScreenshotFileExtension { get; set; } = "jpg";
    public int? ScreenshotHeight { get; set; }
    public string JpgQuality { get; set; } = "2";
    public string WebpQuality { get; set; } = "90";
    public string PngQuality { get; set; } = "6";
}