using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Open.IdentityServer.DynamicClientManagement.Generators;
using Open.IdentityServer.DynamicClientManagement.Generators.Default;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Services.Results;
using Open.IdentityServer.Models;
using Xunit;

namespace Open.IdentityServer.DynamicClientManagement.UnitTests.Generators;

public class DynamicClientManagementResponseGeneratorTests
{
    private readonly IDynamicClientManagementResponseGenerator _subject;

    public DynamicClientManagementResponseGeneratorTests()
    {
        _subject = new DynamicClientManagementResponseGenerator();
    }

    [Fact]
    public async Task generate_when_canceled_should_throw()
    {
        var registrationResult = new DynamicClientManagementClientResult
        {
            Client = new Client
            {
                ClientId = "test-client",
                Properties = new Dictionary<string, string>()
            },
            ClientIdIssuedAt = DateTimeOffset.UtcNow,
            OriginalRequestDto = new RegisterRequestDTO()
        };

        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        var act = async () => await _subject.GenerateRegisterResponseAsync(
            registrationResult,
            cancellationTokenSource.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task generate_response_when_enabled_was_requested_should_include_enabled_in_additional_data()
    {
        var registrationResult = new DynamicClientManagementClientResult
        {
            Client = new Client
            {
                ClientId = "test-client",
                Enabled = false,
                Properties = new Dictionary<string, string>()
            },
            ClientIdIssuedAt = DateTimeOffset.UtcNow,
            OriginalRequestDto = new RegisterRequestDTO
            {
                Enabled = true
            }
        };

        var response = await _subject.GenerateRegisterResponseAsync(
            registrationResult,
            TestContext.Current.CancellationToken);

        response.AdditionalData["enabled"].GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task generate_response_should_return_response()
    {
        var registrationResult = new DynamicClientManagementClientResult
        {
            Client = new Client
            {
                ClientId = "test-client",
                RedirectUris = new[] { "https://example.com/callback" },
                AllowedGrantTypes = new[] { "authorization_code" },
                Properties = new Dictionary<string, string>
                {
                    ["__dcm_property:foo"] = JsonSerializer.Serialize("bar"),
                    ["__dcm_property:bar"] = JsonSerializer.Serialize("foo"),
                    ["__dcm_property:software_id"] = "software-id",
                    ["__dcm_property:software_version"] = "1.0"
                }
            },
            ClientIdIssuedAt = DateTimeOffset.UtcNow,
            ClientSecret = "test-secret",
            ClientSecretExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            RegistrationAccessToken = "access-token",
            RegistrationClientUri = "https://example.com/register",
            OriginalRequestDto = new RegisterRequestDTO
            {
                TokenEndpointAuthMethod = "client_secret_basic",
                ResponseTypes = new[] { "code" }
            },
        };

        var response = await _subject.GenerateRegisterResponseAsync(
            registrationResult,
            TestContext.Current.CancellationToken);

        response.ClientId.Should().Be("test-client");
        response.ClientSecret.Should().Be("test-secret");
        response.ClientIdIssuedAt.Should().Be(registrationResult.ClientIdIssuedAt.ToUnixTimeSeconds());
        response.ClientSecretExpiresAt.Should().Be(registrationResult.ClientSecretExpiresAt?.ToUnixTimeSeconds());
        response.RegistrationAccessToken.Should().Be("access-token");
        response.RegistrationClientUri.Should().Be("https://example.com/register");
        response.RedirectUris.Should().BeEquivalentTo(new[] { "https://example.com/callback" });
        response.GrantTypes.Should().BeEquivalentTo(new[] { "authorization_code" });
        response.TokenEndpointAuthMethod.Should().Be("client_secret_basic");
        response.ResponseTypes.Should().BeEquivalentTo(new[] { "code" });
        response.SoftwareId.Should().Be("software-id");
        response.SoftwareVersion.Should().Be("1.0");
        response.AdditionalData["foo"].GetString().Should().Be("bar");
        response.AdditionalData["bar"].GetString().Should().Be("foo");
        response.AdditionalData.Should().NotContainKey("__dcm_property:foo");
        response.AdditionalData.Should().NotContainKey("__dcm_property:bar");
    }

    [Fact]
    public async Task generate_response_should_map_known_properties_and_exclude_them_from_additional_data()
    {
        var registrationResult = new DynamicClientManagementClientResult
        {
            Client = new Client
            {
                ClientId = "test-client",
                ClientName = "Test Client",
                ClientUri = "https://client.example",
                LogoUri = "https://client.example/logo.png",
                Properties = new Dictionary<string, string>
                {
                    ["__dcm_property:contacts"] = JsonSerializer.Serialize(new[] { "ops@example.com" }),
                    ["__dcm_property:tos_uri"] = "https://client.example/tos",
                    ["__dcm_property:policy_uri"] = "https://client.example/policy",
                    ["__dcm_property:jwks_uri"] = "https://client.example/jwks",
                    ["__dcm_property:jwks"] = JsonSerializer.Serialize(new { keys = Array.Empty<object>() }),
                    ["__dcm_property:software_id"] = "software-id",
                    ["__dcm_property:software_version"] = "1.0",
                    ["__dcm_property:custom_metadata"] = JsonSerializer.Serialize("custom-value")
                }
            },
            ClientIdIssuedAt = DateTimeOffset.UtcNow,
            OriginalRequestDto = new RegisterRequestDTO()
        };

        var response = await _subject.GenerateRegisterResponseAsync(
            registrationResult,
            TestContext.Current.CancellationToken);

        response.ClientName.Should().Be("Test Client");
        response.ClientUri.Should().Be("https://client.example");
        response.LogoUri.Should().Be("https://client.example/logo.png");
        response.Contacts.Should().BeEquivalentTo(new[] { "ops@example.com" });
        response.TermsOfServiceUri.Should().Be("https://client.example/tos");
        response.PolicyUri.Should().Be("https://client.example/policy");
        response.JsonWebKeySetUri.Should().Be("https://client.example/jwks");
        response.JsonWebKeySet.Should().NotBeNull();
        response.JsonWebKeySet!.Value.GetProperty("keys").GetArrayLength().Should().Be(0);
        response.SoftwareId.Should().Be("software-id");
        response.SoftwareVersion.Should().Be("1.0");

        response.AdditionalData["custom_metadata"].GetString().Should().Be("custom-value");
        response.AdditionalData.Should().NotContainKey("contacts");
        response.AdditionalData.Should().NotContainKey("tos_uri");
        response.AdditionalData.Should().NotContainKey("policy_uri");
        response.AdditionalData.Should().NotContainKey("jwks_uri");
        response.AdditionalData.Should().NotContainKey("jwks");
        response.AdditionalData.Should().NotContainKey("software_id");
        response.AdditionalData.Should().NotContainKey("software_version");
        response.AdditionalData.Should().NotContainKey("__dcm_property:custom_metadata");
    }

    [Fact]
    public async Task generate_response_should_ignore_non_dcm_properties()
    {
        var registrationResult = new DynamicClientManagementClientResult
        {
            Client = new Client
            {
                ClientId = "test-client",
                Properties = new Dictionary<string, string>
                {
                    ["non_dcm_property"] = JsonSerializer.Serialize("should-not-be-returned"),
                    ["another_non_dcm_property"] = "not-valid-json-and-should-not-be-parsed",
                    ["__dcm_property:custom_metadata"] = JsonSerializer.Serialize("custom-value")
                }
            },
            ClientIdIssuedAt = DateTimeOffset.UtcNow,
            OriginalRequestDto = new RegisterRequestDTO()
        };

        var response = await _subject.GenerateRegisterResponseAsync(
            registrationResult,
            TestContext.Current.CancellationToken);

        response.AdditionalData["custom_metadata"].GetString().Should().Be("custom-value");
        response.AdditionalData.Should().NotContainKey("non_dcm_property");
        response.AdditionalData.Should().NotContainKey("another_non_dcm_property");
        response.AdditionalData.Should().NotContainKey("__dcm_property:custom_metadata");
    }

    [Fact]
    public async Task generate_response_should_exclude_internal_properties()
    {
        var registrationResult = new DynamicClientManagementClientResult
        {
            Client = new Client
            {
                ClientId = "test-client",
                Properties = new Dictionary<string, string>
                {
                    ["__dcm_property:dcm_token_hash"] = "not-valid-json-and-should-not-be-parsed",
                    ["__dcm_property:custom_metadata"] = JsonSerializer.Serialize("custom-value")
                }
            },
            ClientIdIssuedAt = DateTimeOffset.UtcNow,
            OriginalRequestDto = new RegisterRequestDTO()
        };

        var response = await _subject.GenerateRegisterResponseAsync(
            registrationResult,
            TestContext.Current.CancellationToken);

        response.AdditionalData["custom_metadata"].GetString().Should().Be("custom-value");
        response.AdditionalData.Should().NotContainKey("dcm_token_hash");
        response.AdditionalData.Should().NotContainKey("__dcm_property:dcm_token_hash");
    }

    [Fact]
    public async Task generate_response_should_preserve_additional_metadata_json_types()
    {
        var registrationResult = new DynamicClientManagementClientResult
        {
            Client = new Client
            {
                ClientId = "test-client",
                Properties = new Dictionary<string, string>
                {
                    ["__dcm_property:vendor_string"] = JsonSerializer.Serialize("bar"),
                    ["__dcm_property:vendor_bool"] = JsonSerializer.Serialize(true),
                    ["__dcm_property:vendor_number"] = JsonSerializer.Serialize(10),
                    ["__dcm_property:vendor_array"] = JsonSerializer.Serialize(new[] { "feature-a", "feature-b" }),
                    ["__dcm_property:vendor_object"] = JsonSerializer.Serialize(new { mode = "strict" })
                }
            },
            ClientIdIssuedAt = DateTimeOffset.UtcNow,
            OriginalRequestDto = new RegisterRequestDTO()
        };

        var response = await _subject.GenerateRegisterResponseAsync(
            registrationResult,
            TestContext.Current.CancellationToken);

        response.AdditionalData["vendor_string"].GetString().Should().Be("bar");
        response.AdditionalData["vendor_bool"].GetBoolean().Should().BeTrue();
        response.AdditionalData["vendor_number"].GetInt32().Should().Be(10);
        response.AdditionalData["vendor_array"]
            .EnumerateArray()
            .Select(value => value.GetString())
            .Should()
            .BeEquivalentTo(new[] { "feature-a", "feature-b" });
        response.AdditionalData["vendor_object"].GetProperty("mode").GetString().Should().Be("strict");
    }

    [Fact]
    public async Task generate_response_when_dcm_additional_metadata_is_not_valid_json_should_throw()
    {
        var registrationResult = new DynamicClientManagementClientResult
        {
            Client = new Client
            {
                ClientId = "test-client",
                Properties = new Dictionary<string, string>
                {
                    ["__dcm_property:foo"] = "bar"
                }
            },
            ClientIdIssuedAt = DateTimeOffset.UtcNow,
            OriginalRequestDto = new RegisterRequestDTO()
        };

        var act = async () => await _subject.GenerateRegisterResponseAsync(
            registrationResult,
            TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Client property 'foo' must contain valid JSON metadata.");
    }
}