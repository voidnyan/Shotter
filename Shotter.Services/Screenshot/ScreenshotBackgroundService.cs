using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shotter.Core.Interfaces;
using Shotter.Core.Models;

namespace Shotter.Services.Screenshot;

public sealed partial class ScreenshotBackgroundService(
    ICaptureQueue queue,
    ILogger<ScreenshotBackgroundService> logger,
    IScreenshotProcessor screenshotProcessor,
    INotificationService notificationService)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            CaptureJob? job = null;
            try
            {
                job = await queue.DequeueAsync(stoppingToken);
                queue.SetProcessing(true);

                LogProcesssing(job.MediaPath, job.PositionSeconds, job.CaptureRequest.IncludeSubtitles, job.SubtitlesCodec,
                    job.SubtitleIndex, job.ExternalSubtitlePath, job.OutputPath);

                await ProcessAsync(job, stoppingToken);
                LogProcessingFinished(job.MediaPath, job.PositionSeconds);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {                
                logger.LogError(ex, "Screenshot job failed");
                await SendErrorNotification(job, stoppingToken);
            }
            finally
            {
                queue.SetProcessing(false);
            }
        }
    }

    private async Task ProcessAsync(
        CaptureJob job,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(job.OutPutDirectory);
        await screenshotProcessor.TakeScreenshot(
            cancellationToken,
            job.PositionSeconds,
            job.MediaPath,
            job.OutputPath,
            job.CaptureRequest,
            job.SubtitleIndex,
            job.SubtitlesCodec,
            job.ExternalSubtitlePath);
    }

    private async Task SendErrorNotification(CaptureJob? job, CancellationToken cancellationToken)
    {
        try
        {
            await notificationService.SendNotification(
                $"Failed to process screenshot: {job?.MediaPath} at {job?.PositionSeconds}s.", cancellationToken);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    [LoggerMessage(LogLevel.Information, "Processing screenshot job \n" +
                                         "media path:{MediaPath} \n" +
                                         "timestamp: {PositionSeconds}s \n" +
                                         "subtitles: {IncludeSubtitles} \n" +
                                         "subtitles codec:{SubtitlesCodec} \n" +
                                         "subtitles index: {SubtitlesIndex} \n" +
                                         "external subtitle path: {ExternalSubtitlePath} \n" +
                                         "output path: {OutputPath}")]
    partial void LogProcesssing(string mediaPath, double positionSeconds, bool includeSubtitles, string? subtitlesCodec,
        int? subtitlesIndex, string? externalSubtitlePath, string? outputPath);

    [LoggerMessage(LogLevel.Information, "Finished processing screenshot job for {MediaPath} at {PositionSeconds}s")]
    partial void LogProcessingFinished(string mediaPath, double positionSeconds);
}