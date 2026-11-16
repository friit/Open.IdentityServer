// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Open.IdentityServer.DynamicClientManagement.Configuration;
using Open.IdentityServer.DynamicClientManagement.Exceptions;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Parser.Models;
using Open.IdentityServer.DynamicClientManagement.Validation;

namespace Open.IdentityServer.DynamicClientManagement.Parser.Default;

public class DefaultDynamicClientManagementRequestParser : IDynamicClientManagementRequestParser
{
    private static readonly HashSet<string> JwtRegisteredClaimNames =
    [
        System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Iss,
        System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub,
        System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Aud,
        System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp,
        System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Nbf,
        System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Iat,
        System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti,
    ];

    private readonly IOptions<DynamicClientManagementOptions> _options;
    private readonly ISoftwareStatementValidator _softwareStatementValidator;
    private readonly ILogger<IDynamicClientManagementRequestParser> _logger;
    public DefaultDynamicClientManagementRequestParser(
        IOptions<DynamicClientManagementOptions> options,
        ILogger<IDynamicClientManagementRequestParser> logger)
    {
        _options = options;
        _logger = logger;
    }
    
    public async Task<GetRequestParseResult> ParseGetRequestAsync(HttpContext context, string client_id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        return new GetRequestParseResult()
        {
            ClientId = client_id,
            Token = GetBearerToken(context)
        };
       
    }
    
    private static string? GetBearerToken(HttpContext context)
    {
        var authorization = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrWhiteSpace(authorization))
        {
            return null;
        }

        const string bearerPrefix = "Bearer ";

        if (!authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = authorization[bearerPrefix.Length..].Trim();

        return string.IsNullOrWhiteSpace(token)
            ? null
            : token;
    }
    
    public async Task<RegistrationRequestParseResult> ParseRegisterRequestAsync(Stream body, string? contentType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (!MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue? mediaType) ||
            !string.Equals(
                mediaType.MediaType,
                "application/json",
                StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("Content type '{contentType}' is not supported.", contentType);
            
            throw new DynamicClientManagementParseException(
                Constants.RegisterErrors.InvalidRequest,
                $"Content type '{contentType}' is not supported.");
        }
        
        JsonObject? requestJson;
        try
        {
            requestJson = await JsonSerializer.DeserializeAsync<JsonObject>(
                body,
                _options.Value.ParserJsonSerializerOptions,
                cancellationToken
            );
        }
        catch (Exception exception) when (exception is JsonException or ArgumentNullException)
        {
            _logger.LogError(exception, "Invalid JSON in DCR register request body.");
            
            throw new DynamicClientManagementParseException(
                Constants.RegisterErrors.InvalidRequest,
                $"Invalid JSON: {exception.Message}");
        }

        if (requestJson is null)
        {
            _logger.LogError("Request body must contain a JSON object.");
            
            throw new DynamicClientManagementParseException(
                Constants.RegisterErrors.InvalidRequest,
                "Request body must contain a JSON object.");
        }

        var softwareStatement = GetSoftwareStatement(requestJson);
        if (softwareStatement is null)
        {
            return new RegistrationRequestParseResult()
            {
                RequestDto = DeserializeRequest(requestJson)
            };
        }

        JwtSecurityToken softwareStatementJwt;
        try
        {
            softwareStatementJwt = new JwtSecurityToken(softwareStatement);
        }
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
        {
            _logger.LogError(exception, "Invalid SoftwareStatement.");
            
            throw new DynamicClientManagementParseException(
                Constants.RegisterErrors.InvalidSoftwareStatement,
                $"Invalid SoftwareStatement: {exception.Message}");
        }

        var mergedRequest = ApplySoftwareStatement(requestJson, softwareStatementJwt);

        return new RegistrationRequestParseResult()
        {
            RequestDto = mergedRequest,
            SoftwareStatement = softwareStatementJwt
        };
    }

    private RegisterRequestDTO ApplySoftwareStatement(JsonObject requestJson, JwtSecurityToken softwareStatementJwt)
    {
        JsonObject? softwareStatementJson;
        try
        {
            softwareStatementJson = JsonNode.Parse(Base64UrlEncoder.Decode(softwareStatementJwt.EncodedPayload)) as JsonObject;
        }
        catch (JsonException jsonException)
        {
            throw new DynamicClientManagementParseException(
                Constants.RegisterErrors.InvalidRequest,
                $"Invalid SoftwareStatement payload: {jsonException.Message}");
        }

        if (softwareStatementJson is null)
        {
            throw new DynamicClientManagementParseException(
                Constants.RegisterErrors.InvalidRequest,
                "SoftwareStatement payload must contain a JSON object.");
        }

        foreach (var property in softwareStatementJson)
        {
            if (JwtRegisteredClaimNames.Contains(property.Key))
            {
                continue;
            }

            requestJson[property.Key] = property.Value?.DeepClone();
        }

        return DeserializeRequest(requestJson);
    }

    private static string? GetSoftwareStatement(JsonObject requestJson)
    {
        if (!requestJson.TryGetPropertyValue("software_statement", out var softwareStatement))
        {
            return null;
        }

        if (softwareStatement is null)
        {
            return null;
        }

        if (softwareStatement.GetValueKind() == JsonValueKind.String)
        {
            return softwareStatement.GetValue<string>();
        }

        throw new DynamicClientManagementParseException(
            Constants.RegisterErrors.InvalidRequest,
            "software_statement must be a string.");
    }

    private RegisterRequestDTO DeserializeRequest(JsonNode? requestJson)
    {
        try
        {
            return requestJson.Deserialize<RegisterRequestDTO>(_options.Value.ParserJsonSerializerOptions)
                   ?? throw new DynamicClientManagementParseException(
                       Constants.RegisterErrors.InvalidRequest,
                       "Request body must contain a JSON object.");
        }
        catch (JsonException jsonException)
        {
            throw new DynamicClientManagementParseException(
                Constants.RegisterErrors.InvalidRequest,
                $"Invalid JSON: {jsonException.Message}");
        }
    }
}
