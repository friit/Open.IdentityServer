using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Open.IdentityServer.DynamicClientRegistration.Endpoints;
using Open.IdentityServer.DynamicClientRegistration.Validation;
using Open.IdentityServer.DynamicClientRegistration.Validation.Default;

namespace Open.IdentityServer.DynamicClientRegistration.Configuration.DependencyInjection;

public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Add Dynamic Client Registration Services
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IServiceCollection AddDynamicClientRegistration(this IServiceCollection serviceCollection)
        => serviceCollection.AddDynamicClientRegistration(_ => { });

    public static IServiceCollection AddDynamicClientRegistration(
        this IServiceCollection serviceCollection,
        Action<DynamicClientRegistrationOptions> configureOptions)
    {
        serviceCollection.Configure(configureOptions);
        serviceCollection.AddHttpClient();
        serviceCollection.AddMemoryCache();
        serviceCollection.TryAddScoped<RegisterEndpoint>();
        serviceCollection.TryAddScoped<ISoftwareStatementValidator, DefaultSoftwareStatementValidator>();
        
        return serviceCollection;
    }
}
