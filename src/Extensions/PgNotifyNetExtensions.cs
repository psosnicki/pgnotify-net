using Microsoft.Extensions.DependencyInjection;
using PgNotifyNet.Interfaces;
using PgNotifyNet.Services;
using System.Reflection;

namespace PgNotifyNet.Extensions;

public static class PgNotifyNetExtensions
{
    public static IServiceCollection AddPgNotifyNet(this IServiceCollection serviceCollection, Assembly[] assemblies, string connectionString, Action<IPgNotifyConfigurator> configurator, PgNotifyNetOptions? options = null)
        => AddPgNotify(serviceCollection, assemblies, connectionString, configurator, options);

    public static IServiceCollection AddPgNotifyNet(this IServiceCollection serviceCollection, string connectionString, Action<IPgNotifyConfigurator> configurator, PgNotifyNetOptions? options = null)
        => AddPgNotify(serviceCollection, AppDomain.CurrentDomain.GetAssemblies(), connectionString, configurator, options);

    private static IServiceCollection AddPgNotify(this IServiceCollection serviceCollection, Assembly[] assemblies, string connectionString, Action<IPgNotifyConfigurator> configurator, PgNotifyNetOptions? options = null)
    {
        assemblies.SelectMany(x => x.GetTypes())
              .Where(item => item.GetInterfaces()
              .Where(i => i.IsGenericType).Any(i => i.GetGenericTypeDefinition() == typeof(IHandleNotification<>)) && !item.IsAbstract && !item.IsInterface)
              .ToList()
              .ForEach(assignedType =>
              {
                  var serviceType = assignedType.GetInterfaces().FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IHandleNotification<>));
                  if (serviceType != null)
                      serviceCollection.AddTransient(serviceType, assignedType);
              });

        serviceCollection.AddSingleton<IPgNotificationService, PgNotificationService>();
        serviceCollection.AddSingleton<IPostgresNotifyConfiguration>(_ =>
        {

            var pqNotify = new PgNotifyNetConfiguration(connectionString, options ?? new PgNotifyNetOptions());
            configurator.Invoke(pqNotify);
            return pqNotify;
        });

        serviceCollection.AddHostedService(sp => new PgNotifyNetHostService(sp.GetRequiredService<IPgNotificationService>(), sp.GetRequiredService<IPostgresNotifyConfiguration>()));
        return serviceCollection;
    }
}