using PgNotifyNet;
using PgNotifyNet.Extensions;
using PgNotifyNet.Interfaces;
using PgNotifyNet.Sample.Hubs;
using PgNotifyNet.Sample.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddPgNotifyNet(builder.Configuration.GetConnectionString("SampleDatabase"),
                    c => c.Trigger(t => t.OnTable<Category>(table: "categories", schema: "public").After(Change.Update, Change.Insert, Change.Delete)));

builder.Services.AddSignalR();

var app = builder.Build();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();

app.MapHub<DbNotificationHub>("/dbNotifications");

app.MapFallbackToPage("/_Host");

var notificationService = app.Services.GetRequiredService<IPgNotificationService>();
notificationService.OnDataChange += NotificationService_OnDataChange;

app.Run();


static void NotificationService_OnDataChange(object? sender, OnDataChangeEventArgs e)
{
    Console.WriteLine($"{e.Table} {e.Action} triggered");
}