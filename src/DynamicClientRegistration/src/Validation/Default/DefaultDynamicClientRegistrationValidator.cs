using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.IdentityServer.DynamicClientRegistration.Configuration;
using Open.IdentityServer.DynamicClientRegistration.Models;
using Open.IdentityServer.DynamicClientRegistration.Models.Results;
using Open.IdentityServer.DynamicClientRegistration.Parser.Models;
using Open.IdentityServer.DynamicClientRegistration.Validation.Models;

namespace Open.IdentityServer.DynamicClientRegistration.Validation.Default;

public class DefaultDynamicClientRegistrationValidator : IDynamicClientRegistrationValidator
{
    private readonly IOptions<DynamicClientRegistrationOptions> _options;
    private readonly ISoftwareStatementValidator _softwareStatementValidator;
    private readonly ILogger<IDynamicClientRegistrationValidator> _logger;

    public DefaultDynamicClientRegistrationValidator(
        IOptions<DynamicClientRegistrationOptions> options,
        ISoftwareStatementValidator softwareStatementValidator,
        ILogger<IDynamicClientRegistrationValidator> logger)
    {
        _options = options;
        _softwareStatementValidator = softwareStatementValidator;
        _logger = logger;
    }

    public async Task<RegistrationRequestValidationResult> ValidateAsync(
        RegistrationRequestParseResult parseResult,
        CancellationToken cancellationToken = default)
    {
        _logger.LogTrace("Validating Dynamic Client Registration Request");
        
        cancellationToken.ThrowIfCancellationRequested();

        if (parseResult.Request.RedirectUris == null || !parseResult.Request.RedirectUris.Any())
        {
            _logger.LogError("RedirectUris must be provided.");
            return new RegistrationRequestValidationResult(
                parseResult.Request,
                Constants.RegisterErrors.InvalidClientMetadata,
                "RedirectUris must be provided."
            );
        }
        
        //TODO: Validate Response Type and Grant Type if not null
        //TODO: Reject jwks_uri as it's not a supported feature
        //TODO: Reject subject type pairwise and SectorIdentifierUri as it's not a supported feature'
        //TODO: reject request uris as it's not a supported feature
        //TODO: reject TokenEndpointAuthSigningAlgorithm as it's not a supported feature
        //TODO: reject RequestObjectEncryption and RequestObjectEncryptionAlgorithm
        //todo: reject RequestObjectSigningAlgorithm
        //todo: reject UserInfoEncryptedResponseEncryption and UserInfoEncryptedResponseAlgorithm
        //todo reject UserInfoSignedResponseAlgorithm and IdTokenEncryptedResponseEncryption
        //todo reject IdTokenEncryptedResponseAlgorithm IdTokenSignedResponseAlgorithm
        
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
