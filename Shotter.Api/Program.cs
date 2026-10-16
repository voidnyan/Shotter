using Shotter.Api.Configuration;
using Shotter.Api.Middleware;
using Shotter.Core.Interfaces;
using Shotter.Services;
using Shotter.Services.Screenshot;

var builder = WebApplication.CreateBuilder(args);

builder
    .RegisterConfigurations()
    .RegisterPlaybackProvider()
    .RegisterNotificationProvider();

builder.Services.AddHostedService<ScreenshotBackgroundService>();
builder.Services.AddSingleton<ICaptureQueue, CaptureQueue>();

builder.Services
    .AddTransient<IScreenshotService, ScreenshotService>()
    .AddTransient<IScreenshotProcessor, ScreenshotProcessor>()
    .AddTransient<IFileNameResolver, FileNameResolver>();

builder.Services.AddControllers();
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapControllers();

app.Run();