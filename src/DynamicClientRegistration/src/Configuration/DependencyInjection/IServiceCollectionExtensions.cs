using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Open.IdentityServer.DynamicClientRegistration.Endpoints;

namespace Open.IdentityServer.DynamicClientRegistration.Configuration.DependencyInjection;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Add Dynamic Client Registration Services
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IServiceCollection AddDynamicClientRegistration(this IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddScoped<RegisterEndpoint>();
        
        return serviceCollection;
    }
}