// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Open.IdentityServer.DynamicClientManagement.Endpoints;

namespace Open.IdentityServer.DynamicClientManagement.Configuration.DependencyInjection;

public static class IEndpointRouteBuilderExtensions
{
    public static void MapDynamicClientManagement(this IEndpointRouteBuilder builder)
    {
        var options = builder.ServiceProvider.GetRequiredService<IOptions<DynamicClientManagementOptions>>();

        if (string.IsNullOrWhiteSpace(options.Value.RegisterPath))
        {
            throw new ArgumentException("Route cannot be null or whitespace", nameof(options.Value.RegisterPath));
        }
        
        var managementPath = $"{options.Value.RegisterPath.TrimEnd('/')}/{{clientId}}";

        builder.MapGet(
                managementPath,
                (RegisterEndpoint endpoint, HttpContext context, string clientId) =>
                    endpoint.ProcessGetAsync(context, clientId))
            .WithName("DynamicClientManagementGet")
            .WithDisplayName("Dynamic Client Management Get");
        
        builder.MapPost(
            options.Value.RegisterPath, 
            (RegisterEndpoint endpoint, HttpContext context) => endpoint.ProcessRegistrationAsync(context)
            )
            .WithName("DynamicClientManagement")
            .WithDisplayName("Dynamic Client Management Register");
    }
}