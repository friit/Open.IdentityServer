using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Open.IdentityServer.DynamicClientManagement.EntityFramework.Stores;
using Open.IdentityServer.DynamicClientManagement.Stores;

namespace Open.IdentityServer.DynamicClientManagement.EntityFramework.Configuration;

/// <summary>
/// Extension methods to add EF database support to Open IdentityServer Dynamic Client Management.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Dynamic Client Management Store to the DI system.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddDynamicClientManagementStore(this IServiceCollection services)
    {
        services.TryAddTransient<IDynamicClientManagementStore, DynamicClientManagementStore>();
        return services;
    }
}