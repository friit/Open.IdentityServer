using System.Net;
using Microsoft.AspNetCore.Http;
using Open.IdentityServer.Hosting;

namespace Open.IdentityServer.DynamicClientRegistration.Endpoints.Results;

public class RegisterResult : IEndpointResult
{
    public string Error { get; set; }
    public string ErrorDescription { get; set; }
    
    public HttpStatusCode _statusCode { get; set; }
    
    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = (int)_statusCode;
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";

        await context.Response.WriteAsJsonAsync(new
        {
            error = Error,
            error_description = ErrorDescription
        });
    }
}