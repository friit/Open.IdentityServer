#nullable enable

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Open.IdentityServer.DynamicClientRegistration.Configuration;
using Open.IdentityServer.DynamicClientRegistration.Validation;
using Open.IdentityServer.DynamicClientRegistration.Validation.Default;
using Xunit;

namespace Open.IdentityServer.DynamicClientRegistration.UnitTests.Validation.Default;

public class DefaultSoftwareStatementValidatorTests
{
    private const string Category = "Default Software Statement Signature Validator";

    private readonly DynamicClientRegistrationOptions _options;
    private readonly RsaSecurityKey _signingKey;
    private readonly TestHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly DefaultSoftwareStatementValidator _subject;

    public DefaultSoftwareStatementValidatorTests()
    {
        _options = new DynamicClientRegistrationOptions();
        _signingKey = new RsaSecurityKey(RSA.Create().ExportParameters(true));
        _options.SoftwareStatementSignatureValidationOptions.IssuerSigningKeys["issuer"] = [_signingKey];
        _httpClientFactory = new TestHttpClientFactory();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _subject = new DefaultSoftwareStatementValidator(
            Options.Create(_options),
            _httpClientFactory,
            _cache,
            new NullLogger<ISoftwareStatementValidator>());
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_issuer_has_matching_signing_key_should_succeed()
    {
        var token = CreateSoftwareStatement("issuer", _signingKey);

        var result = await _subject.ValidateAsync(token, CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_issuer_is_missing_should_fail()
    {
        var token = CreateSoftwareStatement(null, _signingKey);

        var result = await _subject.ValidateAsync(token, CancellationToken.None);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_issuer_is_not_configured_should_fail()
    {
        var token = CreateSoftwareStatement("unknown_issuer", _signingKey);

        var result = await _subject.ValidateAsync(token, CancellationToken.None);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_signature_key_does_not_match_should_fail()
    {
        var otherKey = new RsaSecurityKey(RSA.Create().ExportParameters(true));
        var token = CreateSoftwareStatement("issuer", otherKey);

        var result = await _subject.ValidateAsync(token, CancellationToken.None);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_token_is_unsigned_should_fail()
    {
        var token = CreateSoftwareStatement("issuer", signingKey: null);

        var result = await _subject.ValidateAsync(token, CancellationToken.None);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_token_is_expired_and_lifetime_validation_enabled_should_fail()
    {
        var token = CreateSoftwareStatement(
            "issuer",
            _signingKey,
            notBefore: DateTime.UtcNow.AddDays(-2),
            expires: DateTime.UtcNow.AddDays(-1));

        var result = await _subject.ValidateAsync(token, CancellationToken.None);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_token_is_expired_and_lifetime_validation_disabled_should_succeed()
    {
        _options.SoftwareStatementSignatureValidationOptions.ValidateLifetime = false;
        var token = CreateSoftwareStatement(
            "issuer",
            _signingKey,
            notBefore: DateTime.UtcNow.AddDays(-2),
            expires: DateTime.UtcNow.AddDays(-1));

        var result = await _subject.ValidateAsync(token, CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_issuer_has_jwks_uri_with_matching_key_should_succeed()
    {
        _options.SoftwareStatementSignatureValidationOptions.IssuerSigningKeys.Clear();
        _options.SoftwareStatementSignatureValidationOptions.IssuerJwksUris["issuer"] = new Uri("https://issuer.test/jwks");
        _httpClientFactory.Responses.Enqueue(CreateJsonWebKeySet(_signingKey));
        var token = CreateSoftwareStatement("issuer", _signingKey);

        var result = await _subject.ValidateAsync(token, CancellationToken.None);

        result.IsError.Should().BeFalse();
        _httpClientFactory.SendCount.Should().Be(1);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_issuer_has_jwks_uri_should_succeed_and_cache_keys()
    {
        _options.SoftwareStatementSignatureValidationOptions.IssuerSigningKeys.Clear();
        _options.SoftwareStatementSignatureValidationOptions.IssuerJwksUris["issuer"] = new Uri("https://issuer.test/jwks");
        _httpClientFactory.Responses.Enqueue(CreateJsonWebKeySet(_signingKey));
        var token = CreateSoftwareStatement("issuer", _signingKey);

        var firstResult = await _subject.ValidateAsync(token, CancellationToken.None);
        var secondResult = await _subject.ValidateAsync(token, CancellationToken.None);

        firstResult.IsError.Should().BeFalse();
        secondResult.IsError.Should().BeFalse();
        _httpClientFactory.SendCount.Should().Be(1);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_cached_jwks_key_does_not_match_should_succeed_after_single_refresh()
    {
        _options.SoftwareStatementSignatureValidationOptions.IssuerSigningKeys.Clear();
        _options.SoftwareStatementSignatureValidationOptions.IssuerJwksUris["issuer"] = new Uri("https://issuer.test/jwks");
        var oldKey = new RsaSecurityKey(RSA.Create().ExportParameters(true));
        _httpClientFactory.Responses.Enqueue(CreateJsonWebKeySet(oldKey));
        _httpClientFactory.Responses.Enqueue(CreateJsonWebKeySet(_signingKey));
        var token = CreateSoftwareStatement("issuer", _signingKey);

        var result = await _subject.ValidateAsync(token, CancellationToken.None);

        result.IsError.Should().BeFalse();
        _httpClientFactory.SendCount.Should().Be(2);
    }

    private static JwtSecurityToken CreateSoftwareStatement(
        string? issuer,
        SecurityKey? signingKey,
        DateTime? notBefore = null,
        DateTime? expires = null)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            NotBefore = notBefore ?? DateTime.UtcNow.AddMinutes(-1),
            Expires = expires ?? DateTime.UtcNow.AddDays(1),
            Subject = new ClaimsIdentity(
            [
                new Claim("client_name", "client")
            ]),
            SigningCredentials = signingKey is null
                ? null
                : new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256),
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(descriptor);

        return new JwtSecurityToken(handler.WriteToken(token));
    }

    private static string CreateJsonWebKeySet(RsaSecurityKey signingKey)
    {
        var jsonWebKey = JsonWebKeyConverter.ConvertFromRSASecurityKey(signingKey);
        return JsonSerializer.Serialize(new
        {
            keys = new[]
            {
                jsonWebKey
            }
        });
    }

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public Queue<string> Responses { get; } = new();
        public int SendCount { get; private set; }

        public HttpClient CreateClient(string name)
        {
            return new HttpClient(new TestHttpMessageHandler(this));
        }

        private sealed class TestHttpMessageHandler : HttpMessageHandler
        {
            private readonly TestHttpClientFactory _factory;

            public TestHttpMessageHandler(TestHttpClientFactory factory)
            {
                _factory = factory;
            }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                _factory.SendCount++;
                var content = _factory.Responses.Dequeue();

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content)
                });
            }
        }
    }
}
