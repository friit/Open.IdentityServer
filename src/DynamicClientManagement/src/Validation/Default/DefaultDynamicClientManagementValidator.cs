using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.IdentityServer.DynamicClientManagement.Configuration;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Models.Results;
using Open.IdentityServer.DynamicClientManagement.Parser.Models;
using Open.IdentityServer.DynamicClientManagement.Validation.Models;

namespace Open.IdentityServer.DynamicClientManagement.Validation.Default;

public class DefaultDynamicClientManagementValidator : IDynamicClientManagementValidator
{
 
    private delegate string? UnsupportedMetadataDelegate(RegisterRequest request);

    private static readonly UnsupportedMetadataDelegate[] _unsupportedMetadataDelegates =
    [
        r => r.JsonWebKeySetUri is not null ? "jwks_uri" : null,
        r => r.SubjectType is not null ? "subject_type" : null,
        r => r.SectorIdentifierUri is not null ? "sector_identifier_uri" : null,
        r => r.RequestUris is not null ? "request_uris" : null,
        r => r.TokenEndpointAuthSigningAlgorithm is not null ? "token_endpoint_auth_signing_alg" : null, 
        r => r.RequestObjectEncryption is not null ? "request_object_encryption" : null,
        r => r.RequestObjectEncryptionAlgorithm is not null ? "request_object_encryption_alg" : null,
        r => r.RequestObjectSigningAlgorithm is not null ? "request_object_signing_alg" : null,
        r => r.UserInfoEncryptedResponseEncryption is not null ? "userinfo_encrypted_response_enc" : null,
        r => r.UserInfoEncryptedResponseAlgorithm is not null ? "userinfo_encrypted_response_alg" : null,
        r => r.UserInfoSignedResponseAlgorithm is not null ? "userinfo_signed_response_alg" : null,
        r => r.IdTokenEncryptedResponseEncryption is not null ? "id_token_encrypted_response_enc" : null,
        r => r.IdTokenEncryptedResponseAlgorithm is not null ? "id_token_encrypted_response_alg" : null,
        r => r.IdTokenSignedResponseAlgorithm is not null ? "id_token_signed_response_alg" : null,
    ];
    
    private static IReadOnlyList<string> GetUnsupportedMetadata(RegisterRequest request)
    {
        return _unsupportedMetadataDelegates
            .Select(d => d(request))
            .Where(m => m is not null)
            .Select(m => m!)
            .ToList();
    }
    
    private readonly IOptions<DynamicClientManagementOptions> _options;
    private readonly ISoftwareStatementValidator _softwareStatementValidator;
    private readonly ILogger<IDynamicClientManagementValidator> _logger;

    public DefaultDynamicClientManagementValidator(
        IOptions<DynamicClientManagementOptions> options,
        ISoftwareStatementValidator softwareStatementValidator,
        ILogger<IDynamicClientManagementValidator> logger)
    {
        _options = options;
        _softwareStatementValidator = softwareStatementValidator;
        _logger = logger;
    }

    public async Task<RegistrationRequestValidationResult> ValidateAsync(
        RegistrationRequestParseResult parseResult,
        CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Validating Dynamic Client Management Request");
        
        cancellationToken.ThrowIfCancellationRequested();

        var unsupportedMetadata = GetUnsupportedMetadata(parseResult.Request);
        if (unsupportedMetadata.Any())
        {
            var unsupportedMetadataString = string.Join(",\n", unsupportedMetadata);
            _logger.LogError("Unsupported metadata fields: {unsupportedMetadata}", unsupportedMetadataString);
            return new RegistrationRequestValidationResult(
                parseResult.Request,
                Constants.RegisterErrors.InvalidClientMetadata,
                $"Unsupported metadata fields: {unsupportedMetadataString}"
            );
        }
        
        if (parseResult.Request.RedirectUris == null || !parseResult.Request.RedirectUris.Any())
        {
            _logger.LogError("RedirectUris must be provided.");
            return new RegistrationRequestValidationResult(
                parseResult.Request,
                Constants.RegisterErrors.InvalidClientMetadata,
                "RedirectUris must be provided."
            );
        }
        
        
        foreach (var redirectUri in parseResult.Request.RedirectUris)
        {
            if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out Uri? result))
            {
                _logger.LogError("RedirectUri '{redirectUri}' must be an absolute URI.", redirectUri);
                return new RegistrationRequestValidationResult(
                    parseResult.Request,
                    Constants.RegisterErrors.InvalidRedirectUri,
                    $"RedirectUri '{redirectUri}' must be an absolute URI."
                );
            }

            if (result.Scheme == Uri.UriSchemeHttp)
            {
                _logger.LogError("RedirectUri '{redirectUri}' must not use HTTP.", redirectUri);
                return new RegistrationRequestValidationResult(
                    parseResult.Request,
                    Constants.RegisterErrors.InvalidRedirectUri,
                    $"RedirectUri '{redirectUri}' must not use HTTP."
                );
            }
        }

        var frontChannelLogoutValidationResult = ValidateLogoutUri(
            parseResult.Request,
            parseResult.Request.FrontChannelLogoutUri,
            parseResult.Request.FrontChannelLogoutSessionRequired.HasValue,
            "FrontChannelLogoutUri",
            "frontchannel_logout_session_required",
            allowFragment: false);
        if (frontChannelLogoutValidationResult != null)
        {
            return frontChannelLogoutValidationResult;
        }

        var backChannelLogoutValidationResult = ValidateLogoutUri(
            parseResult.Request,
            parseResult.Request.BackChannelLogoutUri,
            parseResult.Request.BackChannelLogoutSessionRequired.HasValue,
            "BackChannelLogoutUri",
            "backchannel_logout_session_required");
        if (backChannelLogoutValidationResult != null)
        {
            return backChannelLogoutValidationResult;
        }

        if (_options.Value.RequireValidSoftwareStatement)
        {
            if (parseResult.SoftwareStatement is null)
            {
                _logger.LogError("SoftwareStatement is required.");
                return new RegistrationRequestValidationResult(
                    parseResult.Request,
                    Constants.RegisterErrors.InvalidSoftwareStatement,
                    "SoftwareStatement is required."
                );
            }
            
            var validationResult = await _softwareStatementValidator.ValidateAsync(
                parseResult.SoftwareStatement,
                cancellationToken
            );

            if (validationResult.IsError)
            {
                _logger.LogError(validationResult.ErrorDescription);
                return new RegistrationRequestValidationResult(
                    parseResult.Request,
                    Constants.RegisterErrors.InvalidSoftwareStatement,
                    validationResult.ErrorDescription
                );
            }
        }
        
        return new RegistrationRequestValidationResult(parseResult.Request);
    }

    private RegistrationRequestValidationResult? ValidateLogoutUri(
        RegisterRequest request,
        string? logoutUri,
        bool hasDependentFlag,
        string propertyName,
        string dependentFlagName,
        bool allowFragment = true)
    {
        if (string.IsNullOrWhiteSpace(logoutUri))
        {
            if (!hasDependentFlag)
            {
                return null;
            }

            var errorDescription = $"{dependentFlagName} requires {propertyName} to be provided.";
            _logger.LogError(errorDescription);
            return new RegistrationRequestValidationResult(
                request,
                Constants.RegisterErrors.InvalidClientMetadata,
                errorDescription);
        }

        if (!Uri.TryCreate(logoutUri, UriKind.Absolute, out var parsedLogoutUri))
        {
            var errorDescription = $"{propertyName} '{logoutUri}' must be an absolute URI.";
            _logger.LogError(errorDescription);
            return new RegistrationRequestValidationResult(
                request,
                Constants.RegisterErrors.InvalidClientMetadata,
                errorDescription);
        }

        if (parsedLogoutUri.Scheme != Uri.UriSchemeHttps)
        {
            var errorDescription = $"{propertyName} '{logoutUri}' must use HTTPS.";
            _logger.LogError(errorDescription);
            return new RegistrationRequestValidationResult(
                request,
                Constants.RegisterErrors.InvalidClientMetadata,
                errorDescription);
        }

        if (!allowFragment && !string.IsNullOrEmpty(parsedLogoutUri.Fragment))
        {
            var errorDescription = $"{propertyName} '{logoutUri}' must not contain a fragment.";
            _logger.LogError(errorDescription);
            return new RegistrationRequestValidationResult(
                request,
                Constants.RegisterErrors.InvalidClientMetadata,
                errorDescription);
        }

        return null;
    }
}
