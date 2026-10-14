using System.ComponentModel.DataAnnotations;

namespace Shotter.Core.Models;

public class CaptureRequest
{
    public bool IncludeSubtitles { get; set; } = false;
    
    [ImageFormat]
    public string? Format { get; set; }
    public int? Height { get; set; }
}

public sealed class ImageFormatAttribute : ValidationAttribute
{
    private static readonly HashSet<string> AllowedFormats =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "jpg", "png", "webp"
        };

    public override bool IsValid(object? value)
    {
        return value is null ||
               value is string format &&
               AllowedFormats.Contains(format.ToLowerInvariant());
    }
}