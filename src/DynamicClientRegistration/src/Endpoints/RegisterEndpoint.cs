using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Open.IdentityServer.Endpoints.Results;
using Open.IdentityServer.Hosting;

namespace Open.IdentityServer.DynamicClientRegistration.Endpoints;

public class RegisterEndpoint : IEndpointHandler
{
    public ILogger<RegisterEndpoint> Logger { get; }

    public RegisterEndpoint(
        ILogger<RegisterEndpoint> logger)
    {
        Logger = logger;
    }
    public async Task<IEndpointResult> ProcessAsync(HttpContext context)
    {
        Logger.LogDebug("Start register request");
        
        IEndpointResult result = null;
        
        if (HttpMethods.IsPost(context.Request.Method))
        { 
            if (!context.Request.HasJsonContentType())
            {
                return new StatusCodeResult(HttpStatusCode.UnsupportedMediaType);
            }
            
            result = await ProcessRegisterPostRequestAsync(context);
        }
        else
        {
            return new StatusCodeResult(HttpStatusCode.MethodNotAllowed);
        }
        
        Logger.LogTrace("End register request. result type: {0}", result?.GetType().ToString() ?? "-none-");

        return result;
    }

    private Task<IEndpointResult> ProcessRegisterGetRequestAsync(HttpContext context)
    {
        throw new NotImplementedException();
    }
    
    private Task<IEndpointResult> ProcessRegisterPostRequestAsync(HttpContext context)
    {
        throw new NotImplementedException();
    }
    
    /*protected async Task<IEndpointResult> CreateErrorResultAsync(
        string logMessage,
        ValidatedAuthorizeRequest request = null,
        string error = OidcConstants.AuthorizeErrors.ServerError,
        string errorDescription = null,
        bool logError = true)
    {
        if (logError)
        {
            Logger.LogError(logMessage);
        }

        if (request != null)
        {
            var details = new AuthorizeRequestValidationLog(request, _options.Logging.AuthorizeRequestSensitiveValuesFilter);
            Logger.LogInformation("{@validationDetails}", details);
        }

        // TODO: should we raise a token failure event for all errors to the authorize endpoint?
        await RaiseFailureEventAsync(request, error, errorDescription);

        return new AuthorizeResult(new AuthorizeResponse
        {
            Request = request,
            Error = error,
            ErrorDescription = errorDescription,
            SessionState = request?.GenerateSessionStateValue()
        });
    }*/
}