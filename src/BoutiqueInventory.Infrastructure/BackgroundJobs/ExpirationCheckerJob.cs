using BoutiqueInventory.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BoutiqueInventory.Infrastructure.BackgroundJobs;

/// <summary>
/// Daily background worker that runs the expiration scan. The job
/// fires once shortly after startup, then again every 24 hours,
/// timed to fall close to the next 08:00 local boundary.
/// </summary>
public sealed class ExpirationCheckerJob(
    IServiceProvider services,
    ILogger<ExpirationCheckerJob> logger) : BackgroundService
{
    /// <summary>Hours past midnight (local time) at which the daily scan fires.</summary>
    private static readonly TimeSpan DailyScanTime = TimeSpan.FromHours(8);

    /// <summary>Lookahead used by the alert generator.</summary>
    private const int WindowDays = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!await DelaySafelyAsync(TimeSpan.FromSeconds(10), stoppingToken)) return;

        await ScanOnceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextRun = ComputeNextRun(DateTimeOffset.Now, DailyScanTime);
            var delay = nextRun - DateTimeOffset.Now;
            if (delay < TimeSpan.Zero) delay = TimeSpan.FromMinutes(1);

            logger.LogInformation(
                "ExpirationCheckerJob next scan scheduled at {NextRun:O} (in {Delay}).",
                nextRun, delay);

            if (!await DelaySafelyAsync(delay, stoppingToken)) return;

            await ScanOnceAsync(stoppingToken);
        }
    }

    private async Task ScanOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = services.CreateScope();
            var monitor = scope.ServiceProvider.GetRequiredService<IExpirationMonitor>();
            var created = await monitor.RunAsync(WindowDays, ct);
            logger.LogInformation(
                "ExpirationCheckerJob ran: created {Count} new alert(s) for products expiring within {Window} days.",
                created, WindowDays);
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ExpirationCheckerJob failed.");
        }
    }

    private static DateTimeOffset ComputeNextRun(DateTimeOffset now, TimeSpan timeOfDay)
    {
        var todayAtTime = new DateTimeOffset(
            now.Year, now.Month, now.Day,
            timeOfDay.Hours, timeOfDay.Minutes, timeOfDay.Seconds,
            now.Offset);

        return todayAtTime > now ? todayAtTime : todayAtTime.AddDays(1);
    }

    private static async Task<bool> DelaySafelyAsync(TimeSpan delay, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, ct);
            return true;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }
}
