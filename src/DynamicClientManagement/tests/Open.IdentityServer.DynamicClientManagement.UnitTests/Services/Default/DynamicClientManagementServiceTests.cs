// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Open.IdentityServer.DynamicClientManagement.Configuration;
using Open.IdentityServer.DynamicClientManagement.Exceptions;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Services.Default;
using Open.IdentityServer.DynamicClientManagement.Stores;
using Open.IdentityServer.Models;
using Open.IdentityServer.Stores;
using Xunit;

namespace Open.IdentityServer.DynamicClientManagement.UnitTests.Services.Default;

public class DynamicClientManagementServiceTests
{
    private const string SharedSecretType = "SharedSecret";
    private const string JsonWebKeySecretType = "JWK";

    private readonly DynamicClientManagementOptions _options;
    private readonly DynamicClientManagementService _subject;
    private readonly IDynamicClientManagementStore _store;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DynamicClientManagementServiceTests()
    {
        _options = new DynamicClientManagementOptions();
        
        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Scheme = "https",
                Host = new HostString("localhost")
            }
        };
        _httpContextAccessor = Mock.Of<IHttpContextAccessor>(a => a.HttpContext == httpContext);
        
        _store = Mock.Of<IDynamicClientManagementStore>();
        _subject = new DynamicClientManagementService(
            _store,
            _httpContextAccessor,
            Options.Create(_options),
            NullLogger<DynamicClientManagementService>.Instance);
    }

    [Fact]
    public async Task register_when_auth_method_is_none_should_not_issue_shared_secret()
    {
        var request = new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            TokenEndpointAuthMethod = "none"
        };

        var result = await _subject.RegisterAsync(request, TestContext.Current.CancellationToken);

        result.ClientSecret.Should().BeNull();
        result.Client.RequireClientSecret.Should().BeFalse();
        result.Client.ClientSecrets.Should().BeEmpty();
        
        Mock.Get(_store)
            .Verify(s => s.RegisterClientAsync(
                It.Is<Client>(c => c.ClientId == result.Client.ClientId), It.IsAny<CancellationToken>())
                ,Times.Once
            );
    }

    [Fact]
    public async Task register_when_auth_method_is_client_secret_basic_should_issue_shared_secret()
    {
        var request = new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            TokenEndpointAuthMethod = "client_secret_basic"
        };

        var result = await _subject.RegisterAsync(request, TestContext.Current.CancellationToken);

        result.ClientSecret.Should().NotBeNullOrWhiteSpace();
        result.Client.RequireClientSecret.Should().BeTrue();
        result.Client.ClientSecrets.Should().ContainSingle();
        result.Client.ClientSecrets.Single().Type.Should().Be(SharedSecretType);
        result.Client.ClientSecrets.Single().Value.Should().Be(HashSharedSecret(result.ClientSecret!));
        
        Mock.Get(_store)
            .Verify(s => s.RegisterClientAsync(
                    It.Is<Client>(c => c.ClientId == result.Client.ClientId), It.IsAny<CancellationToken>())
                ,Times.Once
            );
    }

    [Fact]
    public async Task register_when_auth_method_is_client_secret_post_should_issue_hashed_shared_secret()
    {
        var request = new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            TokenEndpointAuthMethod = "client_secret_post"
        };

        var result = await _subject.RegisterAsync(request, TestContext.Current.CancellationToken);

        result.ClientSecret.Should().NotBeNullOrWhiteSpace();
        result.Client.RequireClientSecret.Should().BeTrue();
        result.Client.ClientSecrets.Should().ContainSingle();
        result.Client.ClientSecrets.Single().Type.Should().Be(SharedSecretType);
        result.Client.ClientSecrets.Single().Value.Should().Be(HashSharedSecret(result.ClientSecret!));
        
        Mock.Get(_store)
            .Verify(s => s.RegisterClientAsync(
                    It.Is<Client>(c => c.ClientId == result.Client.ClientId), It.IsAny<CancellationToken>())
                ,Times.Once
            );
    }

    [Fact]
    public async Task register_when_auth_method_is_private_key_jwt_should_store_jwks_as_client_secrets()
    {
        using var jwksDocument = JsonDocument.Parse("""
        {
          "keys": [
            {
              "kty": "RSA",
              "kid": "key1",
              "use": "sig",
              "n": "abc",
              "e": "AQAB"
            }
          ]
        }
        """);

        var request = new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            TokenEndpointAuthMethod = "private_key_jwt",
            JsonWebKeySet = jwksDocument.RootElement.Clone()
        };

        var result = await _subject.RegisterAsync(request, TestContext.Current.CancellationToken);

        result.ClientSecret.Should().BeNull();
        result.Client.RequireClientSecret.Should().BeTrue();
        result.Client.ClientSecrets.Should().ContainSingle();

        var clientSecret = result.Client.ClientSecrets.Single();
        clientSecret.Type.Should().Be(JsonWebKeySecretType);
        clientSecret.Value.Should().Contain("\"kid\": \"key1\"");
        
        Mock.Get(_store)
            .Verify(s => s.RegisterClientAsync(
                    It.Is<Client>(c => c.ClientId == result.Client.ClientId), It.IsAny<CancellationToken>())
                ,Times.Once
            );
    }

    [Fact]
    public async Task register_when_private_key_jwt_has_no_jwks_should_throw()
    {
        var request = new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            TokenEndpointAuthMethod = "private_key_jwt"
        };

        var act = async () => await _subject.RegisterAsync(request, TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<InvalidDynamicClientManagementRequestException>()
            .WithMessage("jwks must be provided for token_endpoint_auth_method 'private_key_jwt'.");
    }

    [Fact]
    public async Task register_should_create_expected_client()
    {
        // Arrange

        var request = CreateRegisterRequest();
        // Expect client secret to be generated.
        request.TokenEndpointAuthMethod = "client_secret_basic";
        
        string generatedClientId = null!;
        ICollection<Secret> generatedSecrets = [];
        
        // Callback to get the generated values.
        Mock.Get(_store)
            .Setup(s => s.RegisterClientAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()))
            .Callback((Client client, CancellationToken cancellationToken) =>
            {
                generatedClientId = client.ClientId;
                generatedSecrets = client.ClientSecrets;
            });
        
        
        // Act
        var result = await _subject.RegisterAsync(request, TestContext.Current.CancellationToken);
        result.RegistrationAccessToken.Should().NotBeNullOrWhiteSpace();

        // Generate expected token hash for comparison.
        var registrationAccessToken = result.RegistrationAccessToken!;
        var tokenBytes = WebEncoders.Base64UrlDecode(registrationAccessToken);
        var expectedTokenHash = Convert.ToBase64String(SHA256.HashData(tokenBytes));
        
        // Assert
        
        result.Client.ClientId.Should().Be(generatedClientId);
        
        var createdSecret = generatedSecrets.SingleOrDefault(s => s.Type == SharedSecretType);
        createdSecret.Should().NotBeNull();
        
        result.Client.Properties["dcm_token_hash"].Should().Be(expectedTokenHash);
        
        result.Client.ClientName.Should().Be("client_name");
        result.Client.ClientUri.Should().Be("client_uri");
        result.Client.LogoUri.Should().Be("logo_uri");
        result.Client.RedirectUris.Should().BeEquivalentTo(new[] { "https://redirect.test/callback" });
        result.Client.PostLogoutRedirectUris.Should().BeEquivalentTo(new[] { "post_logout_redirect_uri" });
        result.Client.AllowedGrantTypes.Should().BeEquivalentTo(new[] { "grant_type" });
        result.Client.AllowedScopes.Should().BeEquivalentTo(new[] { "scope1", "scope2" });
        result.Client.RequirePkce.Should().BeFalse();
        result.Client.AllowOfflineAccess.Should().BeFalse();
        result.Client.AllowAccessTokensViaBrowser.Should().BeTrue();
        result.Client.FrontChannelLogoutUri.Should().Be("front_channel_logout_uri");
        result.Client.FrontChannelLogoutSessionRequired.Should().BeTrue();
        result.Client.BackChannelLogoutUri.Should().Be("back_channel_logout_uri");
        result.Client.BackChannelLogoutSessionRequired.Should().BeTrue();
        result.Client.UserSsoLifetime.Should().Be(1);
        result.Client.InitiateLoginUri.Should().Be("initiate_login_uri");
        result.Client.Enabled.Should().BeFalse();
        result.Client.RequireConsent.Should().BeTrue();
        result.Client.AllowRememberConsent.Should().BeFalse();
        result.Client.RequireRequestObject.Should().BeTrue();
        result.Client.AlwaysIncludeUserClaimsInIdToken.Should().BeTrue();
        result.Client.IdentityTokenLifetime.Should().Be(1);
        result.Client.AccessTokenLifetime.Should().Be(1);
        result.Client.AuthorizationCodeLifetime.Should().Be(1);
        result.Client.AbsoluteRefreshTokenLifetime.Should().Be(1);
        result.Client.SlidingRefreshTokenLifetime.Should().Be(1);
        result.Client.ConsentLifetime.Should().Be(1);
        result.Client.RefreshTokenUsage.Should().Be(TokenUsage.OneTimeOnly);
        result.Client.UpdateAccessTokenClaimsOnRefresh.Should().BeTrue();
        result.Client.RefreshTokenExpiration.Should().Be(TokenExpiration.Absolute);
        result.Client.AccessTokenType.Should().Be(AccessTokenType.Jwt);
        result.Client.EnableLocalLogin.Should().BeFalse();
        result.Client.IdentityProviderRestrictions.Should().BeEquivalentTo(new[] { "idp1", "idp2" });
        result.Client.IncludeJwtId.Should().BeTrue();
        result.Client.Claims.Should().BeEquivalentTo(new[]
        {
            new ClientClaim("claim1", "value1"),
            new ClientClaim("claim2", "123")
        });
        result.Client.ClientClaimsPrefix.Should().Be("prefix");
        result.Client.PairWiseSubjectSalt.Should().Be("pairwise_subject_salt");
        result.Client.UserCodeType.Should().Be("user_code_type");
        result.Client.DeviceCodeLifetime.Should().Be(1);
        result.Client.AllowedCorsOrigins.Should().BeEquivalentTo(new[] { "https://cors.test" });
        
        result.Client.Properties["contacts"].Should().Be(JsonSerializer.Serialize(new[] { "contact" }));
        result.Client.Properties["tos_uri"].Should().Be("https://client.test/tos");
        result.Client.Properties["policy_uri"].Should().Be("policy_uri");
        result.Client.Properties["software_id"].Should().Be("software_id");
        result.Client.Properties["software_version"].Should().Be("software_version");
    
        result.RegistrationClientUri.Should().Be($"https://localhost/connect/register/{result.Client.ClientId}");
        result.OriginalRequest.Should().Be(request);
    }

    private static string HashSharedSecret(string sharedSecret)
    {
        var bytes = Encoding.UTF8.GetBytes(sharedSecret);
        var hash = SHA256.HashData(bytes);

        return Convert.ToBase64String(hash);
    }

    private static Client CreateClient()
    {
        var properties = new Dictionary<string, string>
        {
            ["contacts"] = JsonSerializer.Serialize(new[] { "contact" }),
            ["tos_uri"] = "https://client.test/tos",
            ["policy_uri"] = "policy_uri",
            ["software_id"] = "software_id",
            ["software_version"] = "software_version"
        };

        return new Client
        {
            ClientName = "client_name",
            ClientUri = "client_uri",
            LogoUri = "logo_uri",
            RedirectUris = ["https://redirect.test/callback"],
            PostLogoutRedirectUris = ["post_logout_redirect_uri"],
            AllowedGrantTypes = ["grant_type"],
            AllowedScopes = ["scope1", "scope2"],
            RequireClientSecret = false,
            RequirePkce = false,
            AllowOfflineAccess = false,
            AllowAccessTokensViaBrowser = true,
            FrontChannelLogoutUri = "front_channel_logout_uri",
            FrontChannelLogoutSessionRequired = true,
            BackChannelLogoutUri = "back_channel_logout_uri",
            BackChannelLogoutSessionRequired = true,
            UserSsoLifetime = 1,
            InitiateLoginUri = "initiate_login_uri",
            Enabled = false,
            RequireConsent = true,
            AllowRememberConsent = false,
            RequireRequestObject = true,
            AlwaysIncludeUserClaimsInIdToken = true,
            IdentityTokenLifetime = 1,
            AccessTokenLifetime = 1,
            AuthorizationCodeLifetime = 1,
            AbsoluteRefreshTokenLifetime = 1,
            SlidingRefreshTokenLifetime = 1,
            ConsentLifetime = 1,
            RefreshTokenUsage = TokenUsage.OneTimeOnly,
            UpdateAccessTokenClaimsOnRefresh = true,
            RefreshTokenExpiration = TokenExpiration.Absolute,
            AccessTokenType = AccessTokenType.Jwt,
            EnableLocalLogin = false,
            IdentityProviderRestrictions = ["idp1", "idp2"],
            IncludeJwtId = true,
            Claims =
            [
                new ClientClaim("claim1", "value1"),
                new ClientClaim("claim2", "123")
            ],
            ClientClaimsPrefix = "prefix",
            PairWiseSubjectSalt = "pairwise_subject_salt",
            UserCodeType = "user_code_type",
            DeviceCodeLifetime = 1,
            AllowedCorsOrigins = ["https://cors.test"],
            Properties = properties
        };
    }
    
    private static RegisterRequest CreateRegisterRequest() 
    {
        return new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            TokenEndpointAuthMethod = "token_endpoint_auth_method",
            GrantTypes = ["grant_type"],
            ResponseTypes = ["response_type"],
            ClientName = "client_name",
            Scope = "scope1 scope2",
            Contacts = ["contact"],
            LogoUri = "logo_uri",
            AdditionalMetadata = new Dictionary<string, JsonElement>
            {
                { "custom_metadata", JsonDocument.Parse("\"value\"").RootElement.Clone() }
            },
            ApplicationType = "web",
            BackChannelLogoutSessionRequired = true,
            FrontChannelLogoutSessionRequired = true,
            FrontChannelLogoutUri = "front_channel_logout_uri",
            BackChannelLogoutUri = "back_channel_logout_uri",
            PostLogoutRedirectUris = ["post_logout_redirect_uri"],
            ClientUri = "client_uri",
            PolicyUri = "policy_uri",
            DefaultAcrValues = ["default_acr_value"],
            DefaultMaxAge = 100,
            RequireAuthTime = true,
            InitiateLoginUri = "initiate_login_uri",
            RequestUris = ["request_uri"],
            SoftwareId = "software_id",
            SoftwareVersion = "software_version",
            SectorIdentifierUri = "sector_identifier_uri",
            SubjectType = "public",
            IdTokenSignedResponseAlgorithm = "id_token_signed_response_alg",
            IdTokenEncryptedResponseAlgorithm = "id_token_encrypted_response_alg",
            IdTokenEncryptedResponseEncryption = "id_token_encrypted_response_enc",
            UserInfoSignedResponseAlgorithm = "userinfo_signed_response_alg",
            UserInfoEncryptedResponseEncryption = "userinfo_encrypted_response_enc",
            RequestObjectSigningAlgorithm = "request_object_signing_alg",
            RequestObjectEncryptionAlgorithm = "request_object_encryption_alg",
            RequestObjectEncryption = "request_object_encryption_enc",
            TokenEndpointAuthSigningAlgorithm = "token_endpoint_auth_signing_alg",
            SoftwareStatement = "software_statement",
            AllowAccessTokensViaBrowser = true,
            Enabled = false,
            RequireConsent = true,
            AllowRememberConsent = false,
            RequirePkce = false,
            RequireRequestObject = true,
            AlwaysIncludeUserClaimsInIdToken = true,
            IdentityTokenLifetime = 1,
            AccessTokenLifetime = 1,
            AuthorizationCodeLifetime = 1,
            AbsoluteRefreshTokenLifetime = 1,
            SlidingRefreshTokenLifetime = 1,
            ConsentLifetime = 1,
            RefreshTokenUsage = "one_time_only",
            UpdateAccessTokenClaimsOnRefresh = true,
            RefreshTokenExpiration = "absolute",
            AccessTokenType = "jwt",
            EnableLocalLogin = false,
            IdentityProviderRestrictions = ["idp1", "idp2"],
            ClientClaims = new Dictionary<string, JsonElement>
            {
                { "claim1", JsonDocument.Parse("\"value1\"").RootElement.Clone() },
                { "claim2", JsonDocument.Parse("123").RootElement.Clone() }
            },
            ClientClaimsPrefix = "prefix",
            PairwiseSubjectSalt = "pairwise_subject_salt",
            UserSsoLifetime = 1,
            DeviceCodeLifetime = 1,
            UserCodeType = "user_code_type",
            AllowedCorsOrigins = ["https://cors.test"],
            JsonWebKeySet = JsonDocument.Parse("""
            {
              "keys": [
                {
                  "kty": "RSA",
                  "kid": "key1",
                  "use": "sig",
                  "n": "abc",
                  "e": "AQAB"
                }
              ]
            }
            """).RootElement.Clone(),
            JsonWebKeySetUri = "https://client.test/jwks",
            IncludeJwtId = true,
            TermsOfServiceUri = "https://client.test/tos",
            UserInfoEncryptedResponseAlgorithm = "userinfo_encrypted_response_alg",

        };
    }
}
