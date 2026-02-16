using CdxEnrich.Actions;
using CdxEnrich.ClearlyDefined;
using Microsoft.Extensions.DependencyInjection;

namespace CdxEnrich
{
    public static class ServiceCollectionExtension
    {
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