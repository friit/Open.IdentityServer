// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Open.IdentityServer.DynamicClientManagement.Configuration;
using Open.IdentityServer.DynamicClientManagement.Models;

namespace Open.IdentityServer.DynamicClientManagement.IntegrationTests;

public class RegisterEndpointPostTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    public RegisterEndpointPostTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();   
    }
    
    [Fact]
    public async Task RegisterEndpoint_WhenPOST_ShouldReturn201ClientMetadataAndRegistrationAccessToken()
    {
        var registerRequest = new RegisterRequestDTO
        {
            RedirectUris = ["https://redirect-uri.test/callback", "https://redirect-uri2.test/callback"],
            TokenEndpointAuthMethod = "client_secret_basic",
            GrantTypes = ["authorization_code"],
            ClientName = "client_name",
            ClientUri = "https://client.test",
            LogoUri = "https://client.test/logo.png",
            Scope = "scope1 scope2",
            Contacts = ["contact1", "contact2"],
            TermsOfServiceUri = "https://client.test/terms",
            PolicyUri = "https://client.test/policy",
            JsonWebKeySetUri = "https://client.test/jwks.json",
            SoftwareId = "software_id",
            SoftwareVersion = "software_version",
            AdditionalMetadata = new Dictionary<string, JsonElement>
            {
                ["custom_metadata"] = JsonDocument.Parse("""{"custom_metadata_value":"custom_metadata_value"}""").RootElement
            }
        };
        
        var response = await _client.PostAsJsonAsync(
            "connect/register", 
            registerRequest, 
            TestContext.Current.CancellationToken);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNullOrEmpty();
        
        using var responseDocument = JsonDocument.Parse(responseContent);
        var responseJson = responseDocument.RootElement;
        
        var clientId = responseJson.GetProperty("client_id").GetString();
        clientId.Should().NotBeNullOrWhiteSpace();
        responseJson.GetProperty("client_secret").GetString().Should().NotBeNullOrWhiteSpace();
        var registrationAccessToken = responseJson.GetProperty("registration_access_token").GetString();
        registrationAccessToken.Should().NotBeNullOrWhiteSpace();
        var registrationClientUri = responseJson.GetProperty("registration_client_uri").GetString();
        registrationClientUri.Should().NotBeNullOrWhiteSpace();
        new Uri(registrationClientUri!).AbsolutePath.Should().Be($"/connect/register/{clientId}");
        responseJson.GetProperty("client_id_issued_at").GetInt64().Should().BePositive();
        responseJson.GetProperty("client_secret_expires_at").GetInt64().Should().BeGreaterThanOrEqualTo(0);
        
        ShouldContainRequestedMetadata(responseJson, registerRequest);

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, registrationClientUri);
        getRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            registrationAccessToken);

        var getResponse = await _client.SendAsync(
            getRequest,
            TestContext.Current.CancellationToken);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var getResponseDocument = JsonDocument.Parse(
            await getResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var getResponseJson = getResponseDocument.RootElement;

        getResponseJson.GetProperty("client_id").GetString().Should().Be(clientId);
        ShouldContainRequestedMetadata(getResponseJson, registerRequest);
    }

    [Fact]
    public async Task RegisterEndpoint_WhenPOSTWithSignedSoftwareStatement_ShouldReturn201ClientMetadataAndRegistrationAccessToken()
    {
        const string issuer = "https://issuer.test";

        using var signingRsa = RSA.Create(2048);
        var validationKey = new RsaSecurityKey(signingRsa.ExportParameters(false));
        var softwareStatement = CreateSignedSoftwareStatement(signingRsa, issuer);

        using var configuredFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.PostConfigure<DynamicClientManagementOptions>(options =>
                {
                    options.RequireValidSoftwareStatement = true;
                    options.SoftwareStatementSignatureValidationOptions.IssuerSigningKeys[issuer] =
                        [validationKey];
                })));
        using var client = configuredFactory.CreateClient();

        var registerRequest = new RegisterRequestDTO
        {
            RedirectUris = ["https://signed-client.test/callback"],
            TokenEndpointAuthMethod = "client_secret_basic",
            GrantTypes = ["authorization_code"],
            ClientName = "unsigned-client-name",
            SoftwareStatement = softwareStatement
        };

        var response = await client.PostAsJsonAsync(
            "connect/register",
            registerRequest,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var responseDocument = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var responseJson = responseDocument.RootElement;

        var clientId = responseJson.GetProperty("client_id").GetString();
        clientId.Should().NotBeNullOrWhiteSpace();
        var registrationAccessToken = responseJson.GetProperty("registration_access_token").GetString();
        registrationAccessToken.Should().NotBeNullOrWhiteSpace();
        var registrationClientUri = responseJson.GetProperty("registration_client_uri").GetString();
        registrationClientUri.Should().NotBeNullOrWhiteSpace();
        new Uri(registrationClientUri!).AbsolutePath.Should().Be($"/connect/register/{clientId}");
        responseJson.GetProperty("client_name").GetString().Should().Be("signed-client-name");
        responseJson.GetProperty("redirect_uris")[0].GetString()
            .Should().Be("https://signed-client.test/callback");

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, registrationClientUri);
        getRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            registrationAccessToken);

        var getResponse = await client.SendAsync(
            getRequest,
            TestContext.Current.CancellationToken);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var getResponseDocument = JsonDocument.Parse(
            await getResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var getResponseJson = getResponseDocument.RootElement;

        getResponseJson.GetProperty("client_id").GetString().Should().Be(clientId);
        getResponseJson.GetProperty("client_name").GetString().Should().Be("signed-client-name");
        getResponseJson.GetProperty("redirect_uris")[0].GetString()
            .Should().Be("https://signed-client.test/callback");
    }

    [Fact]
    public async Task RegisterEndpoint_WhenInvalidRedirectUri_ShouldReturn400()
    {
        var registerRequest = new RegisterRequestDTO()
        {
            RedirectUris = ["invalid_redirect_uri"],
            GrantTypes = ["authorization_code"]
        };
        var response = await _client.PostAsJsonAsync(
            "connect/register", 
            registerRequest, 
            TestContext.Current.CancellationToken);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNullOrEmpty();
        
        using var responseDocument = JsonDocument.Parse(responseContent);
        var errorResponseJson = responseDocument.RootElement;
        
        errorResponseJson.GetProperty("error").GetString().Should()
            .Be("invalid_client_metadata");
        errorResponseJson.GetProperty("error_description").GetString().Should()
            .Be("RedirectUri 'invalid_redirect_uri' must be an absolute URI.");
    }
    
    [Fact]
    public async Task RegisterEndpoint_WhenInvalidClientMetadata_ShouldReturn400()
    {
        var registerRequest = new RegisterRequestDTO
        {
            GrantTypes = ["authorization_code"]
        };
        
        var response = await _client.PostAsJsonAsync(
            "connect/register", 
            registerRequest, 
            TestContext.Current.CancellationToken);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNullOrEmpty();
        
        using var responseDocument = JsonDocument.Parse(responseContent);
        var errorResponseJson = responseDocument.RootElement;
        
        errorResponseJson.GetProperty("error").GetString().Should()
            .Be("invalid_client_metadata");
        errorResponseJson.GetProperty("error_description").GetString().Should()
            .Be("redirect_uris must be provided.");
    }

    private static void ShouldContainRequestedMetadata(JsonElement responseJson, RegisterRequestDTO registerRequestDto)
    {
        var requestJson = JsonSerializer.SerializeToElement(
            registerRequestDto,
            new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        
        foreach (var expectedProperty in requestJson.EnumerateObject())
        {
            responseJson.TryGetProperty(expectedProperty.Name, out var actualProperty)
                .Should()
                .BeTrue($"the response should contain {expectedProperty.Name}");
            
            actualProperty.GetRawText()
                .Should()
                .Be(expectedProperty.Value.GetRawText(), $"{expectedProperty.Name} should match the request");
        }
    }

    private static string CreateSignedSoftwareStatement(RSA signingRsa, string issuer)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateJwtSecurityToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Subject = new ClaimsIdentity(
            [
                new Claim("client_name", "signed-client-name")
            ]),
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(signingRsa),
                SecurityAlgorithms.RsaSha256)
        });

        return tokenHandler.WriteToken(token);
    }
}
