// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.IdentityServer.DynamicClientManagement.Configuration;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Parser.Models;
using Open.IdentityServer.DynamicClientManagement.Services.Results;
using Open.IdentityServer.DynamicClientManagement.Stores;
using Open.IdentityServer.DynamicClientManagement.Validation.Models;
using Open.IdentityServer.DynamicClientManagement.Validation.Models.Errors;
using Open.IdentityServer.Models;

namespace Open.IdentityServer.DynamicClientManagement.Validation.Default;

/// <summary>
/// Validates parsed dynamic client registration requests.
/// </summary>
public class DefaultDynamicClientManagementValidator : IDynamicClientManagementValidator
{
    private readonly IOptions<DynamicClientManagementOptions> _options;
    private readonly ISoftwareStatementValidator _softwareStatementValidator;
    private readonly IDynamicClientManagementStore _dynamicClientManagementStore;
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
        IDynamicClientManagementStore dynamicClientManagementStore,
        ILogger<IDynamicClientManagementValidator> logger)
    {
        _options = options;
        _softwareStatementValidator = softwareStatementValidator;
        _dynamicClientManagementStore = dynamicClientManagementStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RegistrationRequestValidationResult> ValidateRegisterRequestAsync(
        RegistrationRequestParseResult parseResult,
        CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Validating Dynamic Client Management Request");

        cancellationToken.ThrowIfCancellationRequested();

        var validationResult = new RegistrationRequestValidationResult(parseResult.RequestDto);

        ValidateUnsupportedMetadata(parseResult.RequestDto, validationResult);
        ValidateGrantTypes(parseResult.RequestDto, validationResult);
        ValidateRedirectUris(parseResult.RequestDto, validationResult);
        ValidateJsonWebKey(parseResult.RequestDto, validationResult);
        ValidateInitiateLoginUri(parseResult.RequestDto, validationResult);
        ValidateSectorIdentifierUri(parseResult.RequestDto, validationResult);
        ValidatePostLogoutRedirectUris(parseResult.RequestDto, validationResult);
        ValidateFrontChannelLogoutUri(parseResult.RequestDto, validationResult);
        ValidateBackChannelLogoutUri(parseResult.RequestDto, validationResult);
        ValidateDefaultMaxAge(parseResult.RequestDto, validationResult);
        ValidateLifetimes(parseResult.RequestDto, validationResult);

        await ValidateSoftwareStatementAsync(parseResult, validationResult, cancellationToken);

        if (validationResult.IsError)
        {
            _logger.LogError("Dynamic Client Management Request validation failed with errors: {errors}",
                string.Join(",\n ", validationResult.Errors.Select(error => error.ToMessage()))
            );
        }

        return validationResult;
    }

    public Task<bool> ValidateTokenAsync(string client_id, string token)
    {
        throw new NotImplementedException();
    }

    private void ValidateUnsupportedMetadata(RegisterRequestDTO requestDto, RegistrationRequestValidationResult validationResult)
    {
        foreach (var unsupportedMetadataField in GetUnsupportedMetadata(requestDto))
        {
            validationResult.AddError(new UnsupportedMetadataFieldError(unsupportedMetadataField));
        }
    }

    private void ValidateGrantTypes(RegisterRequestDTO requestDto, RegistrationRequestValidationResult validationResult)
    {
        if (requestDto.GrantTypes == null || !requestDto.GrantTypes.Any())
        {
            validationResult.AddError(new GrantTypesRequiredError());
        }
    }

    private void ValidateRedirectUris(RegisterRequestDTO requestDto, RegistrationRequestValidationResult validationResult)
    {
        if (requestDto.RedirectUris == null || !requestDto.RedirectUris.Any())
        {
            validationResult.AddError(new RedirectUrisRequiredError());
            return;
        }
        
        foreach (var uri in requestDto.RedirectUris)
        {
            ValidateUri(uri, "RedirectUri", validationResult);
        }
    }

    private void ValidatePostLogoutRedirectUris(RegisterRequestDTO requestDto, RegistrationRequestValidationResult validationResult)
    {
        if (requestDto.PostLogoutRedirectUris == null || !requestDto.PostLogoutRedirectUris.Any())
        {
            return;
        }

        foreach (var uri in requestDto.PostLogoutRedirectUris)
        {
            ValidateUri(uri, "PostLogoutRedirectUri", validationResult);
        }
    }

    private void ValidateJsonWebKey(RegisterRequestDTO requestDto, RegistrationRequestValidationResult validationResult)
    {
        if (!string.IsNullOrWhiteSpace(requestDto.JsonWebKeySetUri) && requestDto.JsonWebKeySet is not null)
        {
            validationResult.AddError(new JsonWebKeySetDisallowedError());
            return;
        }
        
        if(requestDto.JsonWebKeySet is null && requestDto.TokenEndpointAuthMethod == Constants.TokenEndpointAuthMethod.PrivateKeyJwt)
        {
            validationResult.AddError(new JsonWebKeySetRequiredError());
            return;
        }

        if (!string.IsNullOrWhiteSpace(requestDto.JsonWebKeySetUri))
        {
            ValidateUri(requestDto.JsonWebKeySetUri, "JsonWebKeySetUri", validationResult);
        }

        
    }

    private void ValidateInitiateLoginUri(RegisterRequestDTO requestDto, RegistrationRequestValidationResult validationResult)
    {
        if (string.IsNullOrWhiteSpace(requestDto.InitiateLoginUri))
        {
            return;
        }

        ValidateUri(requestDto.InitiateLoginUri, "InitiateLoginUri", validationResult);
    }

    private void ValidateSectorIdentifierUri(RegisterRequestDTO requestDto, RegistrationRequestValidationResult validationResult)
    {
        if (string.IsNullOrWhiteSpace(requestDto.SectorIdentifierUri))
        {
            return;
        }

        ValidateUri(requestDto.SectorIdentifierUri, "SectorIdentifierUri", validationResult);
    }

    private void ValidateFrontChannelLogoutUri(RegisterRequestDTO requestDto, RegistrationRequestValidationResult validationResult)
    {
        if (requestDto.FrontChannelLogoutSessionRequired == true && string.IsNullOrWhiteSpace(requestDto.FrontChannelLogoutUri))
        {
            validationResult.AddError(new FrontChannelLogoutUriRequiredError());
            return;
        }

        if (string.IsNullOrWhiteSpace(requestDto.FrontChannelLogoutUri))
        {
            return;
        }

        ValidateUri(requestDto.FrontChannelLogoutUri, "FrontChannelLogoutUri", validationResult);
    }

    private void ValidateBackChannelLogoutUri(RegisterRequestDTO requestDto, RegistrationRequestValidationResult validationResult)
    {
        if (requestDto.BackChannelLogoutSessionRequired == true && string.IsNullOrWhiteSpace(requestDto.BackChannelLogoutUri))
        {
            validationResult.AddError(new BackChannelLogoutUriRequiredError());
            return;
        }

        if (string.IsNullOrWhiteSpace(requestDto.BackChannelLogoutUri))
        {
            return;
        }

        ValidateUri(requestDto.BackChannelLogoutUri, "BackChannelLogoutUri", validationResult);
    }

    private void ValidateDefaultMaxAge(RegisterRequestDTO requestDto, RegistrationRequestValidationResult validationResult)
    {
        if (requestDto.DefaultMaxAge is < 0)
        {
            validationResult.AddError(new DefaultMaxAgeInvalidError());
        }
    }

    private void ValidateLifetimes(RegisterRequestDTO requestDto, RegistrationRequestValidationResult validationResult)
    {
        var lifetimes = new[]
        {
            ("identity_token_lifetime", requestDto.IdentityTokenLifetime),
            ("access_token_lifetime", requestDto.AccessTokenLifetime),
            ("authorization_code_lifetime", requestDto.AuthorizationCodeLifetime),
            ("absolute_refresh_token_lifetime", requestDto.AbsoluteRefreshTokenLifetime),
            ("sliding_refresh_token_lifetime", requestDto.SlidingRefreshTokenLifetime),
            ("consent_lifetime", requestDto.ConsentLifetime),
            ("user_sso_lifetime", requestDto.UserSsoLifetime),
            ("device_code_lifetime", requestDto.DeviceCodeLifetime)
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
    protected virtual (string Name, Func<RegisterRequestDTO, bool> IsPresent)[] UnsupportedMetadataRules =>
    [
        
        //("jwks_uri", r => r.JsonWebKeySetUri is not null) TODO Not currently supported by Open.IdentityServer but just want to ignore?,
        ("subject_type", r => r.SubjectType is not null),
        ("response_types", r => r.ResponseTypes is not null),
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
    
    private IReadOnlyList<string> GetUnsupportedMetadata(RegisterRequestDTO requestDto) =>
        UnsupportedMetadataRules
            .Where(r => r.IsPresent(requestDto))
            .Select(r => r.Name)
            .ToList();

}
