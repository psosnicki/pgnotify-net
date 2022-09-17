using Microsoft.Extensions.Logging;
using Npgsql;
using PgNotifyNet.Db;
using PgNotifyNet.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace PgNotifyNet.Services;
public record AdditionalInformation(JsonObject OldData, JsonObject NewData, string Action, string Table);

internal class PgNotificationService : IPgNotificationService
{
    public event EventHandler<OnDataChangeEventArgs>? OnDataChange;

    private readonly IPostgresNotifyConfiguration _postgresNotifyConfiguration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PgNotificationService> _logger;

    public PgNotificationService(IPostgresNotifyConfiguration postgresNotifyConfiguration, IServiceProvider serviceProvider, ILogger<PgNotificationService> logger)
    {
        _postgresNotifyConfiguration = postgresNotifyConfiguration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task CreateTriggers(CancellationToken cancellationToken)
    {
        if (!_postgresNotifyConfiguration.Triggers.Any())
        {
            _logger.LogWarning("There are no triggers defined");
            return;
        }

        await using var connection = new NpgsqlConnection(_postgresNotifyConfiguration.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
        try
        {
            await using var createNotifyChangeCallbackCmd = new NpgsqlCommand(Scripts.CreateNotifyChangeCallback, connection, transaction);
            await createNotifyChangeCallbackCmd.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogInformation("Created pg_notify callback");
            foreach (var notificationTrigger in _postgresNotifyConfiguration.Triggers)
            {
                await using var createTriggerCmd =
                    new NpgsqlCommand(Scripts.CreateTrigger(notificationTrigger.Table, notificationTrigger.Schema,notificationTrigger.On), connection, transaction);
                await createTriggerCmd.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogInformation("Trigger table {table} on {notificationTrigger} ", notificationTrigger.Table, notificationTrigger.On.Select(x => Enum.GetName(typeof(Change), x)?.ToUpper()).Aggregate((p, n) => $"{p} OR {n}"));
            }
            transaction.Commit();
            _logger.LogInformation("Triggers created");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to create triggers");
            transaction.Rollback();
            throw;
        }
    }
    
    public async Task Listen(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_postgresNotifyConfiguration.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var listenCmd = new NpgsqlCommand("LISTEN ondatachange;", connection);
            await listenCmd.ExecuteNonQueryAsync(cancellationToken);
            connection.Notification += async (_, args) => await HandleNotification(args);
            while (!cancellationToken.IsCancellationRequested)
                await connection.WaitAsync(cancellationToken);
            _logger.LogInformation("Listening data changes...");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to listen data changes");
            throw;
        }
    }

    private async Task HandleNotification(NpgsqlNotificationEventArgs args)
    {
        try
        {
            var additionalInformation = JsonSerializer.Deserialize<AdditionalInformation>(args.AdditionalInformation,new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (additionalInformation == null) return;
            _logger.LogInformation("Received {action} notification from {table} table", additionalInformation.Action, additionalInformation.Table);
            _logger.LogDebug("Received notification data {notificationData}", additionalInformation.NewData.ToString());

            OnDataChange?.Invoke(this, new OnDataChangeEventArgs(additionalInformation.OldData, additionalInformation.NewData, additionalInformation.Action, additionalInformation.Table));

            var change = (Change)Enum.Parse(typeof(Change), additionalInformation.Action, ignoreCase: true);
            var trigger = _postgresNotifyConfiguration.Triggers.FirstOrDefault(x => x.Table == additionalInformation.Table && x.On.Contains(change));

            if (trigger == null)
            {
                _logger.LogWarning("There is no trigger defined for table {table} and change {action}", additionalInformation.Table, additionalInformation.Action);
                return;
            }
            await Publish(
                DeserializeNotificationData(additionalInformation.OldData),
                DeserializeNotificationData(additionalInformation.NewData),
                change, 
                trigger.DataType);

            object? DeserializeNotificationData(JsonObject jsonData)
                => jsonData != null ? JsonSerializer.Deserialize(jsonData.ToJsonString(), trigger.DataType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to handle notification {additionalInformation}", args.AdditionalInformation);
        }
    }

    private async Task Publish(object? oldData, object? newData, Change change, Type dataType)
    {
        var genericType = typeof(IHandleNotification<>).MakeGenericType(dataType);
        var subscribers = _serviceProvider.GetServices(genericType);
        if (!subscribers.Any())
        {
            _logger.LogWarning("There is no notification handler defined for {genericTypeName}", genericType.FullName);
            return;
        }
        foreach (var subscriber in subscribers)
            await (Task)genericType
          .GetMethod("OnDataChanged")
          .Invoke(subscriber, parameters: new object[] { oldData, newData, change });
    }
    public async Task RemoveTriggers(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_postgresNotifyConfiguration.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        foreach (var trigger in _postgresNotifyConfiguration.Triggers)
        {
            try
            {
                await using var removeCmd = new NpgsqlCommand(Scripts.RemoveTrigger(trigger.Name, trigger.Table, trigger.Schema), connection);
                await removeCmd.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogInformation("Remove trigger {trigger} on  {schema}.{table}", trigger.Name, trigger.Schema, trigger.Table);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to remove trigger {trigger} from {schema}.{table}", trigger.Name, trigger.Schema, trigger.Table);
                continue;
            }
        }
    }
}
