namespace Shotter.Core.Configuration;

public class FfmpegOptions
{
    public string ScreenshotFileExtension { get; set; } = "jpg";
    public int? ScreenshotHeight { get; set; }
    public string JpgQuality { get; set; } = "2";
    public string WebpQuality { get; set; } = "90";
    public string PngQuality { get; set; } = "6";
    public int GifHeight { get; set; } = 320;
    public int GifFps { get; set; } = 12;
    public int GifMaxLength { get; set; } = 2;
}