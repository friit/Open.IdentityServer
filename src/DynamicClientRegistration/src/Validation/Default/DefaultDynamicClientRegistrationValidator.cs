using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Open.IdentityServer.DynamicClientRegistration.Configuration;
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
}
