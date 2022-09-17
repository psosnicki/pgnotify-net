namespace PgNotifyNet.Builders;

public interface ITriggerOnTable { ITriggerAfter OnTable<TData>(string table, string schema = "public") where TData : class; }
public interface ITriggerAfter { Trigger After(params Change[] changes); }

internal class TriggerBuilder : ITriggerOnTable, ITriggerAfter
{
    private Trigger _trigger;

    public Trigger After(params Change[] changes)
    { 
        _trigger.NotifyOn(changes ?? throw new ArgumentNullException(nameof(changes)));
        return _trigger;
    }
    public ITriggerAfter OnTable<TData>(string tableName, string schema = "public") where TData : class
    {
        _trigger = new Trigger(
            tableName ?? throw new ArgumentNullException(nameof(tableName)),
            schema ?? throw new ArgumentNullException(nameof(schema)),
            typeof(TData));
        return this;
    }
}
