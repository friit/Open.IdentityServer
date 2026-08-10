// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Open.IdentityServer.DynamicClientManagement.Endpoints;

namespace Open.IdentityServer.DynamicClientManagement.Configuration.DependencyInjection;

public static class IEndpointRouteBuilderExtensions
{
    public static RouteHandlerBuilder MapDynamicClientManagement(this IEndpointRouteBuilder builder, string route = "connect/register")
    {
        RouteHandlerBuilder registerBuilder = builder.MapPost(
            route, 
            (RegisterEndpoint endpoint, HttpContext context) => endpoint.ProcessRegistrationAsync(context)
            )
            .WithName("DynamicClientManagement")
            .WithDisplayName("Dynamic Client Management");
       
        return registerBuilder;
    }
}