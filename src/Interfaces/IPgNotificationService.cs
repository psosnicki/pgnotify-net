using System.Text.Json.Nodes;

namespace PgNotifyNet.Interfaces;

public record OnDataChangeEventArgs(JsonObject OldData, JsonObject NewData, string Action, string Table);
public interface IPgNotificationService
{
    event EventHandler<OnDataChangeEventArgs> OnDataChange;
    Task CreateTriggers(CancellationToken cancellationToken);
    Task Listen(CancellationToken cancellationToken);
    Task RemoveTriggers(CancellationToken cancellationToken);
}