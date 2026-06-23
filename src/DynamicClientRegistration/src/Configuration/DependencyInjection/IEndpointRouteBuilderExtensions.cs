using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Open.IdentityServer.DynamicClientRegistration.Endpoints;

namespace Open.IdentityServer.DynamicClientRegistration.Configuration.DependencyInjection;

public static class IEndpointRouteBuilderExtensions
{
    public static RouteHandlerBuilder MapDynamicClientRegistration(this IEndpointRouteBuilder builder, string route = "connect/register")
    {
        RouteHandlerBuilder registerBuilder = builder.MapPost(
            route, 
            (RegisterEndpoint endpoint, HttpContext context) => endpoint.ProcessAsync(context)
            )
            .WithName("DynamicClientRegistration")
            .WithDisplayName("Dynamic Client Registration");
       
        return registerBuilder;
    }
}