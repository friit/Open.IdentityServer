using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Open.IdentityServer.DynamicClientRegistration.Exceptions;
using Open.IdentityServer.DynamicClientRegistration.Generators;
using Open.IdentityServer.DynamicClientRegistration.Models;
using Open.IdentityServer.DynamicClientRegistration.Parser;
using Open.IdentityServer.DynamicClientRegistration.Services;
using Open.IdentityServer.DynamicClientRegistration.Validation;

namespace Open.IdentityServer.DynamicClientRegistration.Endpoints;

public class RegisterEndpoint
{
    private readonly IDynamicClientRegistrationRequestParser _registrationRequestParser;
    private readonly IDynamicClientRegistrationValidator _validator;
    private readonly IDynamicClientRegistrationService _service;
    private readonly IDynamicClientRegistrationResponseGenerator _responseGenerator;
    private readonly ILogger<RegisterEndpoint> _logger;

    public RegisterEndpoint(
        IDynamicClientRegistrationRequestParser registrationRequestParser,
        IDynamicClientRegistrationValidator validator,
        IDynamicClientRegistrationService service,
        IDynamicClientRegistrationResponseGenerator responseGenerator,
        ILogger<RegisterEndpoint> logger)
    {
        _registrationRequestParser = registrationRequestParser;
        _service = service;
        _responseGenerator = responseGenerator;
        _validator = validator;
        _logger = logger;
    }
    public async Task ProcessRegistrationAsync(HttpContext context)
    {
        _logger.LogDebug("Start Dynamic Client Registration request");

        if (!HttpMethods.IsPost(context.Request.Method))
        {
            _logger.LogWarning("Invalid HTTP method for register endpoint: {method}", context.Request.Method);

            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            context.Response.Headers.Allow = HttpMethods.Post;
            return;
        }

        try
        {
            // Parse request
            var request = await _registrationRequestParser.ParseAsync(
                context.Request.Body,
                context.Request.ContentType,
                context.RequestAborted
            );

            // Validate request
            var validationResult = await _validator.ValidateAsync(request, context.RequestAborted);
            if (!validationResult.IsValid())
            {
                _logger.LogWarning("Invalid register request");

                await WriteErrorResultAsync(
                    context,
                    validationResult.Error!,
                    validationResult.ErrorDescription
                );

                return;
            }

            // Register client
            var result = await _service.RegisterAsync(request, context.RequestAborted);

            // Generate response DTO
            var response = await _responseGenerator.GenerateAsync(result, context.RequestAborted);

            // Write response
            await WriteClientInfoResultAsync(context, response);

        }
        catch (DynamicClientManagementException dynamicClientManagementException)
        {
            _logger.LogWarning("Register request failed: {error}", dynamicClientManagementException.Error);

            await WriteErrorResultAsync(
                context,
                dynamicClientManagementException.Error,
                dynamicClientManagementException.ErrorDescription,
                dynamicClientManagementException.StatusCode
            );

            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception processing dynamic client registration request");

            await WriteErrorResultAsync(
                context,
                "server_error",
                "An error occurred processing the request.",
                HttpStatusCode.InternalServerError
            );

            return;
        }
        finally
        {
            _logger.LogTrace("End registration request");
        }
    }
    
    protected async Task WriteClientInfoResultAsync(HttpContext context, RegisterResponse registerResponse)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Created;
        
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
        
        await context.Response.WriteAsJsonAsync(registerResponse);
    }
    
    protected async Task WriteErrorResultAsync(
        HttpContext context, 
        string error, 
        string? errorDescription,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest)
    {
        context.Response.StatusCode = (int)statusCode;
        
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
        
        await context.Response.WriteAsJsonAsync(new
        {
            error,
            error_description = errorDescription
        });
    }
}