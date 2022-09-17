using Microsoft.AspNetCore.SignalR;
using PgNotifyNet.Interfaces;
using PgNotifyNet.Sample.Hubs;
using PgNotifyNet.Sample.Models;

namespace PgNotifyNet.Sample.Notifications;

public class CategoryNotification : IHandleNotification<Category>
{
    private readonly IHubContext<DbNotificationHub> _hubContext;

    public CategoryNotification(IHubContext<DbNotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task OnDataChanged(Category oldData, Category newData, Change change)
    {
        _hubContext.Clients.All.SendAsync("onCategoryUpdated", oldData, newData, change);
        return Task.CompletedTask;
    }
}
