using PgNotifyNet.Builders;

namespace PgNotifyNet;

public record PgNotifyNetOptions(bool RemoveTriggersOnShutdown = false);

public interface IPostgresNotifyConfiguration : IPgNotifyConfigurator
{
    IReadOnlyCollection<Trigger> Triggers { get; }
    string ConnectionString { get; }
    PgNotifyNetOptions Options { get; }
}

public interface IPgNotifyConfigurator
{
    IPgNotifyConfigurator Trigger(Func<ITriggerOnTable, Trigger> triggerBuilder);
}

public class PgNotifyNetConfiguration : IPostgresNotifyConfiguration
{
    public IReadOnlyCollection<Trigger> Triggers => _triggers.AsReadOnly();
    public string ConnectionString { get; }
    public PgNotifyNetOptions Options { get; }

    private readonly List<Trigger> _triggers = new();

    internal PgNotifyNetConfiguration(string connectionString, PgNotifyNetOptions options)
    {
        ConnectionString = connectionString;
        Options = options;
    }

    public IPgNotifyConfigurator Trigger(Func<ITriggerOnTable, Trigger> triggerBuilder)
    {
        _triggers.Add(triggerBuilder.Invoke(new TriggerBuilder()));
        return this;
    }
}