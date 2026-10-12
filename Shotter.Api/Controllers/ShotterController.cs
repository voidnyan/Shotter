using Microsoft.AspNetCore.Mvc;
using Shotter.Core.Interfaces;
using Shotter.Core.Models;
using Shotter.Services.Notifications;

namespace Shotter.Api.Controllers;

[ApiController]
[Route("api")]
public class ShotterController(
    ICaptureQueue captureQueue,
    INotificationService notificationService,
    IScreenshotService screenshotService,
    IClipService clipService)
    : ControllerBase
{
    [HttpGet("screenshot")]
    public async Task<IActionResult> ScreenshotCurrentStream([FromQuery] CaptureRequest request,
        CancellationToken cancellationToken)
    {
        await screenshotService.HandleScreenshotRequest(request, cancellationToken);
        return Accepted();
    }

    [HttpGet("clip")]
    public async Task<IActionResult> ClipCurrentStream(CancellationToken cancellationToken)
    {
        await clipService.ProcessClipRequestAsync(cancellationToken);
        return Accepted();
    }

    [HttpGet("is-processing")]
    public IActionResult IsProcessingScreenshots()
    {
        return Ok(captureQueue.IsProcessing);
    }

    [HttpGet("test-notification")]
    public async Task<IActionResult> TestNotification()
    {
        if (notificationService is NoopNotificationService)
        {
            return BadRequest("You have not configured a notification service.");
        }
        await notificationService.SendNotification("This is a test notification.", CancellationToken.None);
        return Accepted();
    }
}

