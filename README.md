# .NET library to easily subscribe and listen PostgreSQL Database real time notifications

![build](https://github.com/psosnicki/pgnotify-net/actions/workflows/dotnet.yml/badge.svg)

## Install
```
PM> Install-Package PgNotifyNet -Version 1.0.0-beta
```
## Example

[Blazor SignalR Postgresql PgNotifyNet sample application](samples/PgNotifyNet.Sample/README.md)

## Sample Code

Register in container:
```
ConfigureServices(IServicesCollection servicesCollection)
{
        ...
        services.AddPgNotifyNet(connectionString,
                o => o.Trigger (t => t.OnTable<Category>("categories").After(Change.Update, Change.Delete))
                      .Trigger (t => t.OnTable<User>("users").After(Change.Insert))
        ...
);
```
Implement ```IHandleNotification<T>``` and handle notification:
```
    public  class FooNotificationHandler : IHandleNotification<Category>
    {
        public async Task OnDataChanged(Category oldData,Category newData, Change change)
        {
            //handle data change
        }
    }
```

or subscribe to PgNotificationService ```OnDataChange``` event 
```
    public FooController(IPgNotificationService notificationService)
    {
        notificationService.OnDataChange += (object? sender, OnDataChangeEventArgs e) => { 
            //handle notification
        };
    }

```


## License
MIT
