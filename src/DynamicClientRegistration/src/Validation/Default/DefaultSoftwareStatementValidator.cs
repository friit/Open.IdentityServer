using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Open.IdentityServer.DynamicClientRegistration.Configuration;
using Open.IdentityServer.DynamicClientRegistration.Validation.Models;

namespace Open.IdentityServer.DynamicClientRegistration.Validation.Default;

public class DefaultSoftwareStatementValidator : ISoftwareStatementValidator
{
    private const string JwksCacheKeyPrefix = "Open.IdentityServer.DynamicClientRegistration.SoftwareStatementJwks";

    private readonly IOptions<DynamicClientRegistrationOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ISoftwareStatementValidator> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public DefaultSoftwareStatementValidator(
        IOptions<DynamicClientRegistrationOptions> options,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<ISoftwareStatementValidator> logger)
    {
        _options = options;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<SoftwareStatementValidationResult> ValidateAsync(JwtSecurityToken softwareStatement, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Validating Software Statement");
        
        cancellationToken.ThrowIfCancellationRequested();

        if (softwareStatement.RawData is null)
        {
            _logger.LogError("Software Statement must contain a raw data string.");
            throw new ArgumentException("Software Statement must contain a raw data string.", nameof(softwareStatement));
        }

        if (string.IsNullOrWhiteSpace(softwareStatement.Issuer))
        {
            _logger.LogError("Software Statement Issuer is required.");
            return new SoftwareStatementValidationResult(
                softwareStatement,
                Constants.RegisterErrors.InvalidSoftwareStatement, 
                "Issuer is required."
            );
        }
        
        var validationOptions = _options.Value.SoftwareStatementSignatureValidationOptions;
        var hasJwksUri = validationOptions.IssuerJwksUris.ContainsKey(softwareStatement.Issuer);
        var forceJwksRefresh = false;

        while (true)
        {
            var signingKeys = await GetSigningKeysAsync(
                softwareStatement.Issuer,
                validationOptions,
                forceJwksRefresh,
                cancellationToken);

            if (signingKeys.Count == 0)
            {
                _logger.LogError("No signing keys found for Software Statement issuer: {issuer}", softwareStatement.Issuer);
                
                return new SoftwareStatementValidationResult(
                    softwareStatement,
                    Constants.RegisterErrors.InvalidSoftwareStatement,
                    "No signing keys found for issuer: " + softwareStatement.Issuer
                );
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = softwareStatement.Issuer,
                ValidateAudience = false,
                RequireSignedTokens = _options.Value.RequireValidSoftwareStatementSignature,
                ValidateIssuerSigningKey = _options.Value.RequireValidSoftwareStatementSignature,
                IssuerSigningKeys = signingKeys,
                RequireExpirationTime = validationOptions.ValidateLifetime,
                ValidateLifetime = validationOptions.ValidateLifetime,
                ClockSkew = TimeSpan.Zero,
            };

            try
            {
                _tokenHandler.ValidateToken(
                    softwareStatement.RawData,
                    validationParameters,
                    out _);
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogError("Software Statement has expired.");
                return new SoftwareStatementValidationResult(
                    softwareStatement,
                    Constants.RegisterErrors.InvalidSoftwareStatement,
                    "Software Statement has expired."
                );
            }
            catch (SecurityTokenInvalidIssuerException)
            {
                _logger.LogError("Software Statement issuer is invalid.");
                return new SoftwareStatementValidationResult(
                    softwareStatement,
                    Constants.RegisterErrors.InvalidSoftwareStatement,
                    "Software Statement issuer is invalid."
                );
            }
            catch (SecurityTokenInvalidLifetimeException)
            {
                _logger.LogError("Software Statement lifetime is invalid.");
                return new SoftwareStatementValidationResult(
                    softwareStatement,
                    Constants.RegisterErrors.InvalidSoftwareStatement,
                    "Software Statement lifetime is invalid."
                );
            }
            catch (SecurityTokenInvalidSignatureException) when (!forceJwksRefresh && hasJwksUri)
            {
                _logger.LogWarning("Software Statement signature is invalid. Refreshing JWKS.");
                forceJwksRefresh = true;
                continue;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                _logger.LogError("Software Statement signature is invalid.");
                return new SoftwareStatementValidationResult(
                    softwareStatement,
                    Constants.RegisterErrors.InvalidSoftwareStatement,
                    "Software Statement signature is invalid."
                );
            }
            catch (Exception exception)
            {
                _logger.LogError("Invalid Software Statement: {message}", exception.Message);
                return new SoftwareStatementValidationResult(
                    softwareStatement,
                    Constants.RegisterErrors.InvalidSoftwareStatement,
                    $"Invalid Software Statement: {exception.Message}");
            }

            break;
        }
        
        return new SoftwareStatementValidationResult(softwareStatement);
    }

    private async Task<ICollection<SecurityKey>> GetSigningKeysAsync(
        string issuer,
        SoftwareStatementSignatureValidationOptions validationOptions,
        bool forceJwksRefresh,
        CancellationToken cancellationToken)
    {
        var signingKeys = new List<SecurityKey>();
        if (validationOptions.IssuerSigningKeys.TryGetValue(issuer, out var configuredKeys))
        {
            signingKeys.AddRange(configuredKeys);
        }

        var jwksKeys = await GetJsonWebKeySetKeysAsync(
            issuer,
            validationOptions,
            forceJwksRefresh,
            cancellationToken);
        signingKeys.AddRange(jwksKeys);

        return signingKeys;
    }

    private async Task<ICollection<SecurityKey>> GetJsonWebKeySetKeysAsync(
        string issuer,
        SoftwareStatementSignatureValidationOptions validationOptions,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        if (!validationOptions.IssuerJwksUris.TryGetValue(issuer, out var jwksUri))
        {
            return [];
        }

        var cacheKey = $"{JwksCacheKeyPrefix}:{issuer}";
        if (forceRefresh)
        {
            _cache.Remove(cacheKey);
        }

        try
        {
            var keys = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = validationOptions.JwksCacheDuration;

                var httpClient = _httpClientFactory.CreateClient();
                var jwks = await httpClient.GetStringAsync(jwksUri, cancellationToken);
                return (ICollection<SecurityKey>)new JsonWebKeySet(jwks).Keys
                    .Cast<SecurityKey>()
                    .ToList();
            });

            return keys ?? [];
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or ArgumentException or SecurityTokenException)
        {
            return [];
        }
    }
}
