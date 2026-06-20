using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Open.IdentityServer.DynamicClientRegistration.Endpoints;
using Open.IdentityServer.Extensions;
using static Open.IdentityServer.Constants;

namespace Open.IdentityServer.DynamicClientRegistration.Configuration.DependencyInjection;

/// <summary>
/// DI extension methods for adding Dynamic Client Registration
/// </summary>
public static class IdentityServerServiceCollectionExtensions
{
    /// <summary>
    /// Add Dynamic Client Registration
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddDynamicClientRegistration(this IIdentityServerBuilder builder)
    {
        builder
            .AddDynamicClientRegistrationEndpoints();
        
        return builder;
    }
    
    /// <summary>
    /// Add Dynamic Client Registration
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="setupAction"></param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddDynamicClientRegistration(this IIdentityServerBuilder builder, Action<DynamicClientRegistrationOptions> setupAction = null)
    { 
        builder.Services.Configure(setupAction);
        return builder.AddDynamicClientRegistration();
    }
    
    /// <summary>
    /// Add Dynamic Client Registration
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddDynamicClientRegistration(this IIdentityServerBuilder builder, IConfiguration configuration)
    { 
        builder.Services.Configure<DynamicClientRegistrationOptions>(configuration);
        return builder.AddDynamicClientRegistration();
    }
    
    public static IIdentityServerBuilder AddDynamicClientRegistrationEndpoints(this IIdentityServerBuilder builder)
    {
        builder.AddEndpoint<RegisterEndpoint>(EndpointNames.Register, ProtocolRoutePaths.Register.EnsureLeadingSlash());
        return builder;
    }
    
}