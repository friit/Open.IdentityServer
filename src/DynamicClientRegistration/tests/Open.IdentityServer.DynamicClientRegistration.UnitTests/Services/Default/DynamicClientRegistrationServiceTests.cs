using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Open.IdentityServer.DynamicClientRegistration.Exceptions;
using Open.IdentityServer.DynamicClientRegistration.Models;
using Open.IdentityServer.DynamicClientRegistration.Services.Default;
using Open.IdentityServer.Models;
using Open.IdentityServer.Stores;
using Xunit;

namespace Open.IdentityServer.DynamicClientRegistration.UnitTests.Services.Default;

public class DynamicClientRegistrationServiceTests
{
    private const string SharedSecretType = "SharedSecret";
    private const string JsonWebKeySecretType = "JWK";

    private readonly DynamicClientRegistrationService _subject;

    public DynamicClientRegistrationServiceTests()
    {
        _subject = new DynamicClientRegistrationService(
            Mock.Of<IClientStore>(),
            NullLogger<DynamicClientRegistrationService>.Instance);
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
            .ThrowAsync<InvalidDynamicClientRegistrationRequestException>()
            .WithMessage("jwks must be provided for token_endpoint_auth_method 'private_key_jwt'.");
    }

    private static string HashSharedSecret(string sharedSecret)
    {
        var bytes = Encoding.UTF8.GetBytes(sharedSecret);
        var hash = SHA256.HashData(bytes);

        return Convert.ToBase64String(hash);
    }
}
