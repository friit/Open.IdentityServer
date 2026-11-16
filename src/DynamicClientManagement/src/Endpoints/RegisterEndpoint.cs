// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Open.IdentityServer.DynamicClientManagement.Exceptions;
using Open.IdentityServer.DynamicClientManagement.Generators;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Parser;
using Open.IdentityServer.DynamicClientManagement.Services;
using Open.IdentityServer.DynamicClientManagement.Stores;
using Open.IdentityServer.DynamicClientManagement.Validation;
using Open.IdentityServer.Stores;

namespace Open.IdentityServer.DynamicClientManagement.Endpoints;

/// <summary>
/// Processes dynamic client registration requests.
/// </summary>
public class RegisterEndpoint
{
    private readonly IDynamicClientManagementStore _clientStore;
    private readonly IDynamicClientManagementRequestParser _registrationRequestParser;
    private readonly IDynamicClientManagementValidator _validator;
    private readonly IDynamicClientManagementService _service;
    private readonly IDynamicClientManagementResponseGenerator _responseGenerator;
    private readonly ILogger<RegisterEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterEndpoint"/> class.
    /// </summary>
    /// <param name="registrationRequestParser">Parses incoming registration requests.</param>
    /// <param name="validator">Validates parsed registration requests.</param>
    /// <param name="service">Performs client registration.</param>
    /// <param name="responseGenerator">Creates the registration response payload.</param>
    /// <param name="logger">The logger used to write diagnostic messages.</param>
    public RegisterEndpoint(
        IDynamicClientManagementRequestParser registrationRequestParser,
        IDynamicClientManagementValidator validator,
        IDynamicClientManagementService service,
        IDynamicClientManagementResponseGenerator responseGenerator,
        IDynamicClientManagementStore clientStore,
        ILogger<RegisterEndpoint> logger)
    {
        _registrationRequestParser = registrationRequestParser;
        _service = service;
        _clientStore = clientStore;
        _responseGenerator = responseGenerator;
        _validator = validator;
        _logger = logger;
    }

    public async Task ProcessGetAsync(HttpContext context, string clientId)
    {
        _logger.LogDebug("Start Dynamic Client GET Management request");
        
        if(!HttpMethods.IsGet(context.Request.Method))
        {
            _logger.LogWarning("Invalid HTTP method for register endpoint: {method}", context.Request.Method);

            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            context.Response.Headers.Allow = HttpMethods.Get;
            return;
        }
        
        if(string.IsNullOrWhiteSpace(clientId))
        {
            _logger.LogWarning("Missing client id for register endpoint");

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        
        var request = await _registrationRequestParser.ParseGetRequestAsync(context, clientId, context.RequestAborted);

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            _logger.LogError("Missing bearer token for register endpoint");
            await WriteErrorResultAsync(
                context,
                Constants.RegisterErrors.MissingBearerToken,
                "Dynamic Client Management Bearer token is required for GET requests."
            );
            return;
        }
        
        var client = await _clientStore.FindClientByIdAsync(request.ClientId);
        if(client == null)
        {
            _logger.LogWarning("Invalid client id for register endpoint: {clientId}", request.ClientId);
            await WriteErrorResultAsync(
                context,
                Constants.RegisterErrors.InvalidClient,
                "Client not found",
                HttpStatusCode.NotFound
            );
            return;
        }

        var validationResult =  await _validator.ValidateTokenAsync(request.ClientId, request.Token);
        if(!validationResult)
        {
            _logger.LogError("Invalid bearer token for register endpoint");
            await WriteErrorResultAsync(
                context,
                Constants.RegisterErrors.InvalidBearerToken,
                "Invalid Dynamic Client Management Bearer token for client."

            );
            return;
        }
   
        // Generate response DTO
        var response = await _responseGenerator.GenerateGetResponseAsync(client, context.RequestAborted);

        // Write response
        await WriteOkResultAsync(context, response);
    }
    
    /// <summary>
    /// Processes a dynamic client registration request.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task ProcessRegistrationAsync(HttpContext context)
    {
        _logger.LogDebug("Start Dynamic Client POST Management request");

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
            var parseResult = await _registrationRequestParser.ParseRegisterRequestAsync(
                context.Request.Body,
                context.Request.ContentType,
                context.RequestAborted
            );

            // Validate request
            var validationResult = await _validator.ValidateRegisterRequestAsync(parseResult, context.RequestAborted);
            if (validationResult.IsError)
            {
                _logger.LogError("Invalid register request {errors}", string.Join(",\n ", validationResult.Errors.Select(error => error.ToMessage())));

                var validationError = validationResult.Errors[0];
                var errorDescription = string.Join(
                    ",\n ",
                    validationResult.Errors
                        .Select(error => error.ErrorDescription)
                        .Where(description => !string.IsNullOrWhiteSpace(description)));

                await WriteErrorResultAsync(
                    context,
                    validationError.Error,
                    string.IsNullOrWhiteSpace(errorDescription) ? null : errorDescription
                );

                return;
            }

            // Register client
            var result = await _service.RegisterAsync(parseResult.RequestDto, context.RequestAborted);

            // Generate response DTO
            var response = await _responseGenerator.GenerateRegisterResponseAsync(result, context.RequestAborted);

            // Write response
            await WriteCreatedResultAsync(context, response);

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

    /// <summary>
    /// Writes a successful registration response to the current HTTP response.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="clientResponseDto">The response payload to return.</param>
    protected Task WriteCreatedResultAsync(HttpContext context, ClientResponseDTO clientResponseDto) =>
        WriteJsonResultAsync(context, clientResponseDto, HttpStatusCode.Created);

    /// <summary>
    /// Writes a successful client management response to the current HTTP response.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="clientResponseDto">The response payload to return.</param>
    protected Task WriteOkResultAsync(HttpContext context, ClientResponseDTO clientResponseDto) =>
        WriteJsonResultAsync(context, clientResponseDto, HttpStatusCode.OK);

    private async Task WriteJsonResultAsync(
        HttpContext context,
        ClientResponseDTO clientResponseDto,
        HttpStatusCode statusCode)
    {
        context.Response.StatusCode = (int)statusCode;

        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";

        await context.Response.WriteAsJsonAsync(clientResponseDto);
    }
    
    /// <summary>
    /// Writes an error response to the current HTTP response.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="error">The error code to return.</param>
    /// <param name="errorDescription">An optional error description.</param>
    /// <param name="statusCode">The HTTP status code to return.</param>
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