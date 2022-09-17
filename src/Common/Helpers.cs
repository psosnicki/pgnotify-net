namespace PgNotifyNet.Common;

internal static class Helpers
{
    public static string CreateTriggerName(this Change[] changes, string table, string schema)
        => "pgNotifyNet_OnDataChange_" + (!changes.Any() ? new[] { Change.Update, Change.Delete, Change.Insert } : changes)
            .Select(x => Enum.GetName(typeof(Change), x)?.ToUpper()).Aggregate((p, n) => $"{p} OR {n}")
            ?.Replace(" ", "_") + $"_{schema}_{table}";
}
