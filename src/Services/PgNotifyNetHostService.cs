using Microsoft.Extensions.Hosting;
using PgNotifyNet.Interfaces;

namespace PgNotifyNet.Services;

internal class PgNotifyNetHostService : BackgroundService
{
    private readonly IPgNotificationService _notificationService;
    private readonly IPostgresNotifyConfiguration _postgresNotifyConfiguration;

    public PgNotifyNetHostService(IPgNotificationService notificationService, IPostgresNotifyConfiguration postgresNotifyConfiguration)
    {
        _notificationService = notificationService;
        _postgresNotifyConfiguration = postgresNotifyConfiguration;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _notificationService.CreateTriggers(stoppingToken);
        await _notificationService.Listen(stoppingToken);
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_postgresNotifyConfiguration.Options.RemoveTriggersOnShutdown)
            await _notificationService.RemoveTriggers(cancellationToken);
    }
}