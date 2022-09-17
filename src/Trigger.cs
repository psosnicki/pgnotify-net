using PgNotifyNet.Common;

namespace PgNotifyNet;
public class Trigger
{
    public string Table { get; }
    public string Schema { get; }
    public Type DataType { get; }
    public Change[] On { get; private set; }
    public string Name => $"{On.CreateTriggerName(Table, Schema)}";
    public Trigger(string table, string schema, Type dataType)
    {
        Table = table;
        Schema = schema;
        DataType = dataType;
        On = Array.Empty<Change>();
    }
    public void NotifyOn(params Change[] changes)
    {
        On = changes;
    }
}
