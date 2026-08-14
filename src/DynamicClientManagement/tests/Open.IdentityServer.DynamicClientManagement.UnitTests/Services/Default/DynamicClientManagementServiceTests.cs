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
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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

    private readonly DynamicClientManagementService _subject;
    private readonly IDynamicClientManagementStore _store;

    public DynamicClientManagementServiceTests()
    {
        _store = Mock.Of<IDynamicClientManagementStore>();
        _subject = new DynamicClientManagementService(
            _store,
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
        var request = CreateRegisterRequest();
        request.TokenEndpointAuthMethod = "none";

        var result = await _subject.RegisterAsync(request, TestContext.Current.CancellationToken);

        result.Client.Should().BeEquivalentTo(
            CreateClient(),
            options => options.Excluding(client => client.ClientId));
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
            Properties = new Dictionary<string, string>
            {
                ["contacts"] = JsonSerializer.Serialize(new[] { "contact" }),
                ["terms_of_service_uri"] = "https://client.test/tos",
                ["policy_uri"] = "policy_uri",
                ["software_id"] = "software_id",
                ["software_version"] = "software_version"
            }
        };
    }
    
    private static RegisterRequest CreateRegisterRequest() 
    {
        return new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            TokenEndpointAuthMethod = "",
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
