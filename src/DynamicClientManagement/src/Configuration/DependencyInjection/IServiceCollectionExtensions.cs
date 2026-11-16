// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Open.IdentityServer.DynamicClientManagement.Endpoints;
using Open.IdentityServer.DynamicClientManagement.Generators;
using Open.IdentityServer.DynamicClientManagement.Generators.Default;
using Open.IdentityServer.DynamicClientManagement.Parser;
using Open.IdentityServer.DynamicClientManagement.Parser.Default;
using Open.IdentityServer.DynamicClientManagement.Services;
using Open.IdentityServer.DynamicClientManagement.Services.Default;
using Open.IdentityServer.DynamicClientManagement.Stores;
using Open.IdentityServer.DynamicClientManagement.Stores.Default;
using Open.IdentityServer.DynamicClientManagement.Validation;
using Open.IdentityServer.DynamicClientManagement.Validation.Default;
using Open.IdentityServer.Models;
using Open.IdentityServer.Stores;

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
        serviceCollection.AddHttpContextAccessor();

        serviceCollection.TryAddSingleton<ICollection<Client>>(_ => []);
        serviceCollection.TryAddSingleton<IClientStore>(serviceProvider =>
            new InMemoryDynamicClientStore(serviceProvider.GetRequiredService<ICollection<Client>>()));
        serviceCollection.TryAddSingleton<IDynamicClientManagementStore, InMemoryDynamicClientManagementStore>();
        serviceCollection.TryAddScoped<IDynamicClientManagementRequestParser, DefaultDynamicClientManagementRequestParser>();
        serviceCollection.TryAddScoped<IDynamicClientManagementValidator, DefaultDynamicClientManagementValidator>();
        serviceCollection.TryAddScoped<IDynamicClientManagementService, DynamicClientManagementService>();
        serviceCollection.TryAddScoped<IDynamicClientManagementResponseGenerator, DynamicClientManagementResponseGenerator>();
        serviceCollection.TryAddScoped<RegisterEndpoint>();
        serviceCollection.TryAddScoped<ISoftwareStatementValidator, DefaultSoftwareStatementValidator>();
        
        return serviceCollection;
    }
}
