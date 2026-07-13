using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Open.IdentityServer.DynamicClientManagement.Models;

namespace Open.IdentityServer.DynamicClientManagement.IntegrationTests;

public class RegisterEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private HttpClient _client;

    public RegisterEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();   
    }
    
    [Fact]
    public async Task RegisterEndpoint_WhenGET_ShouldReturn404()
    {
        var response = await _client.GetAsync("/register", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RegiterEndpoint_WhenPOST_ShouldReturn201AndClientMetadata()
    {
        var registerRequest = new RegisterRequest
        {
            RedirectUris = ["https://redirect_uri.test, https://redirect_uri2.test"],
            TokenEndpointAuthMethod = "token_endpoint_auth_method",
            GrantTypes = ["grant_type, grant_type2"],
            ResponseTypes = ["response_type, response_type2"],
            ClientName = "client_name",
            ClientUri = "client_uri",
            LogoUri = "logo_uri",
            Scope = "scope scope2", 
            Contacts = ["contact1", "contact2"],
            TermsOfServiceUri = "terms_of_service_uri",
            PolicyUri = "policy_uri",
            JsonWebKeySetUri = "jwks_uri",
            JsonWebKeySet = JsonDocument.Parse("""{"keys":[{"kty":"RSA","use":"sig","kid":"example-key-id","n":"example-modulus","e":"AQAB"}]}""").RootElement,
            SoftwareId = "software_id",
            SoftwareVersion = "software_version",
            ApplicationType = "application_type", 
            SectorIdentifierUri = "sector_identifier_uri",
            SubjectType = "subject_type",
            IdTokenSignedResponseAlgorithm = "id_token_signed_response_alg",
            IdTokenEncryptedResponseAlgorithm = "id_token_encrypted_response_alg",
            IdTokenEncryptedResponseEncryption = "id_token_encrypted_response_enc",
            UserInfoSignedResponseAlgorithm = "userinfo_signed_response_alg",
            UserInfoEncryptedResponseEncryption = "userinfo_encrypted_response_enc",
            RequestObjectSigningAlgorithm = "request_object_signing_alg",
            RequestObjectEncryptionAlgorithm = "request_object_encryption_alg",
            RequestObjectEncryption = "request_object_encryption_enc",
            TokenEndpointAuthSigningAlgorithm = "token_endpoint_auth_signing_alg",
            DefaultMaxAge = 100,
            RequireAuthTime = true,
            DefaultAcrValues = ["arc", "acr2"],
            InitiateLoginUri = "initiate_login_uri",
            RequestUris = ["request_uri", "request_uri2"],
            PostLogoutRedirectUris = ["post_logout_redirect_uri", "post_logout_redirect_uri2"],
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
        
        responseJson.GetProperty("client_id").GetString().Should().NotBeNullOrWhiteSpace();
        responseJson.GetProperty("client_secret").GetString().Should().NotBeNullOrWhiteSpace();
        responseJson.GetProperty("registration_access_token").GetString().Should().NotBeNullOrWhiteSpace();
        responseJson.GetProperty("registration_client_uri").GetString().Should().NotBeNullOrWhiteSpace();
        responseJson.GetProperty("client_id_issued_at").GetInt64().Should().BePositive();
        responseJson.GetProperty("client_secret_expires_at").GetInt64().Should().BeGreaterThanOrEqualTo(0);
        
        ShouldContainRequestedMetadata(responseJson, registerRequest);
    }

    [Fact]
    public async Task RegisterEndpoint_WhenInvalidRedirectUri_ShouldReturn400()
    {
        var registerRequest = new RegisterRequest()
        {
            RedirectUris = ["invalid_redirect_uri"]
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
        
        errorResponseJson.GetProperty("error").GetString().Should().Be("invalid_redirect_uri");
        errorResponseJson.GetProperty("error_description").GetString().Should().Be("RedirectUri 'invalid_redirect_uri' must be an absolute URI.");
    }
    
    [Fact]
    public async Task RegisterEndpoint_WhenInvalidClientMetadata_ShouldReturn400()
    {
        var registerRequest = new RegisterRequest();
        
        var response = await _client.PostAsJsonAsync(
            "connect/register", 
            registerRequest, 
            TestContext.Current.CancellationToken);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        responseContent.Should().NotBeNullOrEmpty();
        
        using var responseDocument = JsonDocument.Parse(responseContent);
        var errorResponseJson = responseDocument.RootElement;
        
        errorResponseJson.GetProperty("error").GetString().Should().Be("invalid_client_metadata");
        errorResponseJson.GetProperty("error_description").GetString().Should().Be("RedirectUris must be provided.");
    }

    private static void ShouldContainRequestedMetadata(JsonElement responseJson, RegisterRequest registerRequest)
    {
        var requestJson = JsonSerializer.SerializeToElement(
            registerRequest,
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
}
