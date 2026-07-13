using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Open.IdentityServer.DynamicClientManagement.Endpoints;
using Open.IdentityServer.DynamicClientManagement.Validation;
using Open.IdentityServer.DynamicClientManagement.Validation.Default;

namespace Open.IdentityServer.DynamicClientManagement.Configuration.DependencyInjection;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Add Dynamic Client Management Services
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IServiceCollection AddDynamicClientManagement(this IServiceCollection serviceCollection)
        => serviceCollection.AddDynamicClientManagement(_ => { });

    public static IServiceCollection AddDynamicClientManagement(
        this IServiceCollection serviceCollection,
        Action<DynamicClientManagementOptions> configureOptions)
    {
        serviceCollection.Configure(configureOptions);
        serviceCollection.AddHttpClient();
        serviceCollection.AddMemoryCache();
        serviceCollection.TryAddScoped<RegisterEndpoint>();
        serviceCollection.TryAddScoped<ISoftwareStatementValidator, DefaultSoftwareStatementValidator>();
        
        return serviceCollection;
    }
}
