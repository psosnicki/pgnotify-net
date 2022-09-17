using PgNotifyNet.Common;

namespace PgNotifyNet.Db;
internal static class Scripts
{
    internal static string PgNotifyNetSchema = "pgNotifyNet";
    internal static string CreateNotifyChangeCallback => $@"
        CREATE SCHEMA IF NOT EXISTS {PgNotifyNetSchema};
        CREATE OR REPLACE FUNCTION {PgNotifyNetSchema}.""NotifyChange""()
        RETURNS trigger
        LANGUAGE 'plpgsql'
        AS $BODY$ 
        DECLARE
            oldData JSON;
            data JSON;
            notification JSON;

        BEGIN
            IF(TG_OP = 'DELETE') THEN
            data = row_to_json(OLD);
            oldData = row_to_json(OLD);
        ELSE
            data = row_to_json(NEW);
            oldData = row_to_json(OLD);
        END IF;
            notification = json_build_object(
                'table', TG_TABLE_NAME,
                'action', TG_OP,
                'oldData', oldData,
                'newData', data);  
              
            PERFORM pg_notify('ondatachange', notification::TEXT);
        RETURN NEW;
        END
        $BODY$;";

    internal static string CreateTrigger(string table, string schema, params Change[] updateOn)
    {
        var notifyAfter = (!updateOn.Any() ? new[] { Change.Update, Change.Delete, Change.Insert } : updateOn).Select(x => Enum.GetName(typeof(Change), x)?.ToUpper()).Aggregate((p, n) => $"{p} OR {n}");
        var triggerName = Helpers.CreateTriggerName(updateOn, table, schema);
        return $@"DROP TRIGGER IF EXISTS ""{triggerName}"" ON ""{schema}"".""{table}""; CREATE TRIGGER ""{triggerName}""
                    AFTER {notifyAfter} 
                    ON ""{schema}"".""{table}""
                    FOR EACH ROW
                EXECUTE PROCEDURE {PgNotifyNetSchema}.""NotifyChange""();";
    }

    internal static string RemoveTrigger(string triggerName, string table, string schema)
        => $@"DROP TRIGGER IF EXISTS ""{triggerName}"" ON ""{schema}"".""{table}""";
}

