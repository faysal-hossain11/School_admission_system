using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Workers;

public sealed class AdmissionHoldExpirationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<AdmissionHoldExpirationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var holdService = scope.ServiceProvider.GetRequiredService<IAdmissionHoldService>();
                await holdService.ExpireDueHoldsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (NpgsqlException exception)
            {
                logger.LogError(exception, "PostgreSQL error while expiring due admission holds.");
            }
            catch (DbUpdateException exception)
            {
                logger.LogError(exception, "Database update error while expiring due admission holds.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
