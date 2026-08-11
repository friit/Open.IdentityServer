// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

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

/// <summary>
/// Processes dynamic client registration requests.
/// </summary>
public class RegisterEndpoint
{
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
        ILogger<RegisterEndpoint> logger)
    {
        _registrationRequestParser = registrationRequestParser;
        _service = service;
        _responseGenerator = responseGenerator;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Processes a dynamic client registration request.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
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
                _logger.LogError("Invalid register request {errors}", string.Join(",\n ", validationResult.Errors));

                await WriteErrorResultAsync(
                    context,
                    string.Join(",\n ", validationResult.Errors),
                    null
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

    /// <summary>
    /// Writes a successful registration response to the current HTTP response.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="registerResponse">The response payload to return.</param>
    protected async Task WriteClientInfoResultAsync(HttpContext context, RegisterResponse registerResponse)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Created;

        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";

        await context.Response.WriteAsJsonAsync(registerResponse);
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