// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.IdentityServer.DynamicClientManagement.Configuration;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Models.Results;
using Open.IdentityServer.DynamicClientManagement.Parser.Models;
using Open.IdentityServer.DynamicClientManagement.Validation.Models;
using Open.IdentityServer.DynamicClientManagement.Validation.Models.Errors;

namespace Open.IdentityServer.DynamicClientManagement.Validation.Default;

/// <summary>
/// Validates parsed dynamic client registration requests.
/// </summary>
public class DefaultDynamicClientManagementValidator : IDynamicClientManagementValidator
{
    private readonly IOptions<DynamicClientManagementOptions> _options;
    private readonly ISoftwareStatementValidator _softwareStatementValidator;
    private readonly ILogger<IDynamicClientManagementValidator> _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDynamicClientManagementValidator"/> class.
    /// </summary>
    /// <param name="options">The dynamic client management options.</param>
    /// <param name="softwareStatementValidator">Validates supplied software statements.</param>
    /// <param name="logger">The logger used to write diagnostic messages.</param>
    public DefaultDynamicClientManagementValidator(
        IOptions<DynamicClientManagementOptions> options,
        ISoftwareStatementValidator softwareStatementValidator,
        ILogger<IDynamicClientManagementValidator> logger)
    {
        _options = options;
        _softwareStatementValidator = softwareStatementValidator;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RegistrationRequestValidationResult> ValidateAsync(
        RegistrationRequestParseResult parseResult,
        CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Validating Dynamic Client Management Request");

        cancellationToken.ThrowIfCancellationRequested();

        var validationResult = new RegistrationRequestValidationResult(parseResult.Request);

        ValidateUnsupportedMetadata(parseResult.Request, validationResult);
        ValidateGrantTypes(parseResult.Request, validationResult);
        ValidateRedirectUris(parseResult.Request, validationResult);
        ValidateJsonWebKey(parseResult.Request, validationResult);
        ValidateInitiateLoginUri(parseResult.Request, validationResult);
        ValidateSectorIdentifierUri(parseResult.Request, validationResult);
        ValidatePostLogoutRedirectUris(parseResult.Request, validationResult);
        ValidateFrontChannelLogoutUri(parseResult.Request, validationResult);
        ValidateBackChannelLogoutUri(parseResult.Request, validationResult);
        ValidateDefaultMaxAge(parseResult.Request, validationResult);
        ValidateLifetimes(parseResult.Request, validationResult);

        await ValidateSoftwareStatementAsync(parseResult, validationResult, cancellationToken);

        if (validationResult.IsError)
        {
            _logger.LogError("Dynamic Client Management Request validation failed with errors: {errors}",
                string.Join(",\n ", validationResult.Errors.Select(error => error.ToMessage()))
            );
        }

        return validationResult;
    }

    private void ValidateUnsupportedMetadata(RegisterRequest request, RegistrationRequestValidationResult validationResult)
    {
        foreach (var unsupportedMetadataField in GetUnsupportedMetadata(request))
        {
            validationResult.AddError(new UnsupportedMetadataFieldError(unsupportedMetadataField));
        }
    }

    private void ValidateGrantTypes(RegisterRequest request, RegistrationRequestValidationResult validationResult)
    {
        if (request.GrantTypes == null || !request.GrantTypes.Any())
        {
            validationResult.AddError(new GrantTypesRequiredError());
        }
    }

    private void ValidateRedirectUris(RegisterRequest request, RegistrationRequestValidationResult validationResult)
    {
        if (request.RedirectUris == null || !request.RedirectUris.Any())
        {
            validationResult.AddError(new RedirectUrisRequiredError());
            return;
        }
        
        foreach (var uri in request.RedirectUris)
        {
            ValidateUri(uri, "RedirectUri", validationResult);
        }
    }

    private void ValidatePostLogoutRedirectUris(RegisterRequest request, RegistrationRequestValidationResult validationResult)
    {
        if (request.PostLogoutRedirectUris == null || !request.PostLogoutRedirectUris.Any())
        {
            return;
        }

        foreach (var uri in request.PostLogoutRedirectUris)
        {
            ValidateUri(uri, "PostLogoutRedirectUri", validationResult);
        }
    }

    private void ValidateJsonWebKey(RegisterRequest request, RegistrationRequestValidationResult validationResult)
    {
        if (!string.IsNullOrWhiteSpace(request.JsonWebKeySetUri) && request.JsonWebKeySet is not null)
        {
            validationResult.AddError(new JsonWebKeySetDisallowedError());
            return;
        }
        
        if(request.JsonWebKeySet is null && request.TokenEndpointAuthMethod == Constants.TokenEndpointAuthMethod.PrivateKeyJwt)
        {
            validationResult.AddError(new JsonWebKeySetRequiredError());
            return;
        }

        ValidateUri(request.JsonWebKeySetUri, "JsonWebKeySetUri", validationResult);

        
    }

    private void ValidateInitiateLoginUri(RegisterRequest request, RegistrationRequestValidationResult validationResult)
    {
        if (string.IsNullOrWhiteSpace(request.InitiateLoginUri))
        {
            return;
        }

        ValidateUri(request.InitiateLoginUri, "InitiateLoginUri", validationResult);
    }

    private void ValidateSectorIdentifierUri(RegisterRequest request, RegistrationRequestValidationResult validationResult)
    {
        if (string.IsNullOrWhiteSpace(request.SectorIdentifierUri))
        {
            return;
        }

        ValidateUri(request.SectorIdentifierUri, "SectorIdentifierUri", validationResult);
    }

    private void ValidateFrontChannelLogoutUri(RegisterRequest request, RegistrationRequestValidationResult validationResult)
    {
        if (request.FrontChannelLogoutSessionRequired == true && string.IsNullOrWhiteSpace(request.FrontChannelLogoutUri))
        {
            validationResult.AddError(new FrontChannelLogoutUriRequiredError());
            return;
        }

        if (string.IsNullOrWhiteSpace(request.FrontChannelLogoutUri))
        {
            return;
        }

        ValidateUri(request.FrontChannelLogoutUri, "FrontChannelLogoutUri", validationResult);
    }

    private void ValidateBackChannelLogoutUri(RegisterRequest request, RegistrationRequestValidationResult validationResult)
    {
        if (request.BackChannelLogoutSessionRequired == true && string.IsNullOrWhiteSpace(request.BackChannelLogoutUri))
        {
            validationResult.AddError(new BackChannelLogoutUriRequiredError());
            return;
        }

        if (string.IsNullOrWhiteSpace(request.BackChannelLogoutUri))
        {
            return;
        }

        ValidateUri(request.BackChannelLogoutUri, "BackChannelLogoutUri", validationResult);
    }

    private void ValidateDefaultMaxAge(RegisterRequest request, RegistrationRequestValidationResult validationResult)
    {
        if (request.DefaultMaxAge is < 0)
        {
            validationResult.AddError(new DefaultMaxAgeInvalidError());
        }
    }

    private void ValidateLifetimes(RegisterRequest request, RegistrationRequestValidationResult validationResult)
    {
        var lifetimes = new[]
        {
            ("identity_token_lifetime", request.IdentityTokenLifetime),
            ("access_token_lifetime", request.AccessTokenLifetime),
            ("authorization_code_lifetime", request.AuthorizationCodeLifetime),
            ("absolute_refresh_token_lifetime", request.AbsoluteRefreshTokenLifetime),
            ("sliding_refresh_token_lifetime", request.SlidingRefreshTokenLifetime),
            ("consent_lifetime", request.ConsentLifetime),
            ("user_sso_lifetime", request.UserSsoLifetime),
            ("device_code_lifetime", request.DeviceCodeLifetime)
        };

        foreach (var (name, value) in lifetimes)
        {
            if (value == 0)
            {
                validationResult.AddError(new LifetimeInvalidError(name));
            }
        }
    }

    private async Task ValidateSoftwareStatementAsync(
        RegistrationRequestParseResult parseResult,
        RegistrationRequestValidationResult validationResult,
        CancellationToken cancellationToken)
    {
        if (_options.Value.RequireValidSoftwareStatement && parseResult.SoftwareStatement is null)
        {
            validationResult.AddError(new SoftwareStatementRequiredError());
            return;
        }

        if (parseResult.SoftwareStatement is null)
        {
            return;
        }

        var softwareStatementValidationResult = await _softwareStatementValidator.ValidateAsync(
            parseResult.SoftwareStatement,
            cancellationToken
        );

        if (softwareStatementValidationResult.IsError)
        {
            validationResult.AddError(softwareStatementValidationResult.Error!);
        }
    }
    
    private void ValidateUri(
        string uri,
        string uriName,
        RegistrationRequestValidationResult validationResult)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? result))
        {
            validationResult.AddError(new UriMustBeAbsoluteError(uriName, uri));
            return;
        }

        if (result.Scheme != Uri.UriSchemeHttps)
        {
            validationResult.AddError(new UriMustUseHttpsError(uriName, uri));
            return;
        }

        if (!string.IsNullOrEmpty(result.Fragment))
        {
            validationResult.AddError(new UriMustNotContainFragmentError(uriName, uri));
        }
    }
    
    /// <summary>
    /// Metadata fields that are not supported by Open.IdentityServer from the Dynamic Client Management specification.
    /// Override this property to add or move unsupported metadata fields.
    /// </summary>
    protected virtual (string Name, Func<RegisterRequest, bool> IsPresent)[] UnsupportedMetadataRules =>
    [
        ("subject_type", r => r.SubjectType is not null),
        ("request_uris", r => r.RequestUris is not null),
        ("token_endpoint_auth_signing_alg", r => r.TokenEndpointAuthSigningAlgorithm is not null),
        ("request_object_encryption", r => r.RequestObjectEncryption is not null),
        ("request_object_encryption_alg", r => r.RequestObjectEncryptionAlgorithm is not null),
        ("request_object_signing_alg", r => r.RequestObjectSigningAlgorithm is not null),
        ("userinfo_encrypted_response_enc", r => r.UserInfoEncryptedResponseEncryption is not null),
        ("userinfo_encrypted_response_alg", r => r.UserInfoEncryptedResponseAlgorithm is not null),
        ("userinfo_signed_response_alg", r => r.UserInfoSignedResponseAlgorithm is not null),
        ("id_token_encrypted_response_enc", r => r.IdTokenEncryptedResponseEncryption is not null),
        ("id_token_encrypted_response_alg", r => r.IdTokenEncryptedResponseAlgorithm is not null),
        ("id_token_signed_response_alg", r => r.IdTokenSignedResponseAlgorithm is not null),
    ];
    
    private IReadOnlyList<string> GetUnsupportedMetadata(RegisterRequest request) =>
        UnsupportedMetadataRules
            .Where(r => r.IsPresent(request))
            .Select(r => r.Name)
            .ToList();

}
