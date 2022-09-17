namespace PgNotifyNet.Interfaces;

public interface IHandleNotification<in TData> where TData : class
{
    public Task OnDataChanged(TData oldData, TData newData, Change change);
}