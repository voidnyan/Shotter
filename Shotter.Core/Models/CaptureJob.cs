namespace Shotter.Core.Models;

public sealed record CaptureJob(
    string MediaPath,
    double PositionSeconds,
    CaptureRequest CaptureRequest,
    int? SubtitleIndex,
    string? SubtitlesCodec,
    string? ExternalSubtitlePath,
    string OutputPath,
    string OutPutDirectory);