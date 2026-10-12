using Shotter.Core.Enums;

namespace Shotter.Core.Models;

public sealed record CaptureJob(
    CaptureType CaptureType,
    string MediaPath,
    double PositionSeconds,
    CaptureRequest CaptureRequest,
    int? SubtitleIndex,
    string? SubtitlesCodec,
    string? ExternalSubtitlePath,
    string OutputPath,
    string OutPutDirectory,
    double? EndPositionSeconds = null);