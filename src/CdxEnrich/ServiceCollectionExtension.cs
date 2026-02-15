using CdxEnrich.Actions;
using CdxEnrich.ClearlyDefined;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CdxEnrich
{
    public static class ServiceCollectionExtension
    {
        internal static void AddLogging(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging(x =>
            {
                x.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
                x.SetMinimumLevel(LogLevel.Information);
                x.AddFilter("System.Net.Http.HttpClient", LogLevel.None);
            });
        }

        internal static void AddReplaceLicenseByClearlyDefined(this ServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<ReplaceLicenseByClearlyDefined>();
            serviceCollection.AddTransient<IClearlyDefinedClient, ClearlyDefinedClient>();
            serviceCollection.AddHttpClient(nameof(ClearlyDefinedClient),
                client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(60);
                    client.BaseAddress = ClearlyDefinedClient.ClearlyDefinedApiBaseAddress;
                }
            );
        }
    }
}