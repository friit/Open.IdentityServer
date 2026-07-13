using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Open.IdentityServer.DynamicClientManagement.Exceptions;
using Open.IdentityServer.DynamicClientManagement.Generators;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Parser;
using Open.IdentityServer.DynamicClientManagement.Services;
using Open.IdentityServer.DynamicClientManagement.Validation;

namespace Open.IdentityServer.DynamicClientManagement.Endpoints;

public class RegisterEndpoint
{
    private readonly IDynamicClientManagementRequestParser _registrationRequestParser;
    private readonly IDynamicClientManagementValidator _validator;
    private readonly IDynamicClientManagementService _service;
    private readonly IDynamicClientManagementResponseGenerator _responseGenerator;
    private readonly ILogger<RegisterEndpoint> _logger;

    public RegisterEndpoint(
        IDynamicClientManagementRequestParser registrationRequestParser,
        IDynamicClientManagementValidator validator,
        IDynamicClientManagementService service,
        IDynamicClientManagementResponseGenerator responseGenerator,
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
        _logger.LogDebug("Start Dynamic Client Management request");

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
            var parseResult = await _registrationRequestParser.ParseAsync(
                context.Request.Body,
                context.Request.ContentType,
                context.RequestAborted
            );

            // Validate request
            var validationResult = await _validator.ValidateAsync(parseResult, context.RequestAborted);
            if (validationResult.IsError)
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
            var result = await _service.RegisterAsync(parseResult.Request, context.RequestAborted);

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
            _logger.LogError(ex, "Unexpected exception processing dynamic client management request");

            await WriteErrorResultAsync(
                context,
                Constants.RegisterErrors.ServerError,
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