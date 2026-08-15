using System;
using System.Threading.Tasks;
using AwesomeAssertions;
using Open.IdentityServer.DynamicClientManagement.Generators;
using Open.IdentityServer.DynamicClientManagement.Generators.Default;
using Open.IdentityServer.DynamicClientManagement.Models.Results;
using Xunit;

namespace Open.IdentityServer.DynamicClientManagement.UnitTests.Generators;

public class DynamicClientManagementResponseGeneratorTests
{
    IDynamicClientManagementResponseGenerator _subject;

    public DynamicClientManagementResponseGeneratorTests()
    {
        _subject = new DynamicClientManagementResponseGenerator();
    }

    [Fact]
    public async Task generate_when_canceled_should_throw()
    {
        
    }

    [Fact]
    public async Task generate_response_should_return_response()
    {
        var registrationResult = new DynamicClientManagementResult()
        {
            Client = new Open.IdentityServer.Models.Client
            {
                ClientId = "test-client",
                RedirectUris = new[] { "https://example.com/callback" },
                AllowedGrantTypes = new[] { "authorization_code" }
            },
            ClientIdIssuedAt = DateTimeOffset.UtcNow,
            ClientSecret = "test-secret",
            ClientSecretExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            RegistrationAccessToken = "access-token",
            RegistrationClientUri = "https://example.com/register",
            OriginalRequest = new Models.RegisterRequest
            {
                TokenEndpointAuthMethod = "client_secret_basic",
                ResponseTypes = new[] { "code" }
            }
        };
        
        var response = await _subject.GenerateAsync(registrationResult, TestContext.Current.CancellationToken);
        
        response.ClientId.Should().Be("test-client");
        response.ClientSecret.Should().Be("test-secret");
        response.ClientIdIssuedAt.Should().Be(registrationResult.ClientIdIssuedAt.ToUnixTimeSeconds());
        response.ClientSecretExpiresAt.Should().Be(registrationResult.ClientSecretExpiresAt?.ToUnixTimeSeconds());
        response.RegistrationAccessToken.Should().Be("access-token");
        response.RegistrationClientUri.Should().Be("https://example.com/register"); 
        
    }

    
    

}