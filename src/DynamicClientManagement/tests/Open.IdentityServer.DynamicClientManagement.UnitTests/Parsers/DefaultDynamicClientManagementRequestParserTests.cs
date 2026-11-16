// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Open.IdentityServer.DynamicClientManagement.Configuration;
using Open.IdentityServer.DynamicClientManagement.Exceptions;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Parser;
using Open.IdentityServer.DynamicClientManagement.Parser.Default;
using Xunit;

namespace Open.IdentityServer.DynamicClientManagement.UnitTests.Parsers;

public class DefaultDynamicClientManagementRequestParserTests
{
    private const string Category = "Default Dynamic Client Management Request Parser";
    
    private HttpContext _context;
    private DynamicClientManagementOptions _options;
    private DefaultDynamicClientManagementRequestParser _subject;
    private RegisterRequestDTO requestDto;
    
    public DefaultDynamicClientManagementRequestParserTests()
    {
        Init();
    }
    
    [Fact]
    [Trait("Category", Category)]
    public async Task parse_when_redirect_uris_is_empty_should_return_request()
    {
        requestDto = new RegisterRequestDTO();
        
        SetJsonBody(requestDto);
        
        var result = await _subject.ParseRegisterRequestAsync(
            _context.Request.Body,
            _context.Request.ContentType,
            _context.RequestAborted);
        
        result.RequestDto.Should().BeEquivalentTo(requestDto);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task parse_when_invalid_json_expect_dcr_exception()
    {
        SetJsonBody("invalid json");
        
        var task = async () => await _subject.ParseRegisterRequestAsync(
            _context.Request.Body,
            _context.Request.ContentType,
            _context.RequestAborted);
        
        await task.Should()
            .ThrowAsync<DynamicClientManagementParseException>()
            .WithMessage("Invalid JSON:*");
    }
    
    [Fact]
    [Trait("Category", Category)]
    public async Task parse_when_body_is_empty_expect_dcr_exception()
    {
        var task = async () => await _subject.ParseRegisterRequestAsync(
            new MemoryStream(),
            _context.Request.ContentType,
            _context.RequestAborted);

        await task.Should()
            .ThrowAsync<DynamicClientManagementParseException>()
            .WithMessage("Invalid JSON: The input does not contain any JSON tokens*");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task parse_when_content_type_is_not_json_expect_dcr_exception()
    {
        _context.Request.ContentType = "text/plain";
        
        var task = async () => await _subject.ParseRegisterRequestAsync(
            _context.Request.Body,
            _context.Request.ContentType,
            _context.RequestAborted);

        await task.Should()
            .ThrowAsync<DynamicClientManagementParseException>()
            .WithMessage("Content type 'text/plain' is not supported.");
    }
    
    [Fact]
    [Trait("Category", Category)]
    public async Task parse_when_content_type_is_json_with_charset_should_return_valid_request()
    {
        SetJsonBody(requestDto);
        _context.Request.ContentType = "application/json; charset=utf-8";

        var result = await _subject.ParseRegisterRequestAsync(
            _context.Request.Body,
            _context.Request.ContentType,
            _context.RequestAborted);

        result.RequestDto.Should().BeEquivalentTo(requestDto);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task parse_when_software_statement_is_invalid_expect_dcr_exception()
    {
        typeof(RegisterRequestDTO).GetProperty(nameof(RegisterRequestDTO.SoftwareStatement))!
            .SetValue(requestDto, "invalid_software_statement");
        
        SetJsonBody(requestDto);
        
        var task = async () => await _subject.ParseRegisterRequestAsync(
            _context.Request.Body,
            _context.Request.ContentType,
            _context.RequestAborted);

        await task.Should()
            .ThrowAsync<DynamicClientManagementParseException>()
            .WithMessage("Invalid SoftwareStatement:*");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task parse_when_request_contains_software_statement_should_return_software_statement()
    {
        var request = new RegisterRequestDTO
        {
            SoftwareStatement = CreateSoftwareStatement(requestDto)
        };

        SetJsonBody(request);

        var result = await _subject.ParseRegisterRequestAsync(
            _context.Request.Body,
            _context.Request.ContentType,
            _context.RequestAborted);

        result.SoftwareStatement.Should().NotBeNull();
    }
    
    [Fact]
    [Trait("Category", Category)]
    public async Task parse_when_valid_should_return_valid_request()
    {
        SetJsonBody(requestDto);
        
        var result = await _subject.ParseRegisterRequestAsync(
            _context.Request.Body,
            _context.Request.ContentType,
            _context.RequestAborted);
        
        result.Should().NotBeNull();
        result.RequestDto.Should().BeEquivalentTo(requestDto);   
    }
    
    [Fact]
    [Trait("Category", Category)]
    public async Task parse_when_request_contains_software_statement_should_return_valid_request()
    {
        var topLevelRequest = new RegisterRequestDTO()
        {
            RedirectUris = ["_"],
            TokenEndpointAuthMethod = "_",
            GrantTypes = ["_"],
            ResponseTypes = ["_"],
            ClientName = "_",
            ClientUri = "_",
            LogoUri = "_",
            Scope = "_",
            Contacts = ["_"],
            TermsOfServiceUri = "_",
            PolicyUri = "_",
            JsonWebKeySetUri = "_",
            SoftwareId = "_",
            SoftwareVersion = "_",
            SoftwareStatement = CreateSoftwareStatement(requestDto),
            ApplicationType = "_",
            SectorIdentifierUri = "_",
            SubjectType = "_",
            IdTokenSignedResponseAlgorithm = "_",
            IdTokenEncryptedResponseAlgorithm = "_",
            IdTokenEncryptedResponseEncryption = "_",
            UserInfoSignedResponseAlgorithm = "_",
            UserInfoEncryptedResponseEncryption = "_",
            RequestObjectSigningAlgorithm = "_",
            RequestObjectEncryptionAlgorithm = "_",
            RequestObjectEncryption = "_",
            TokenEndpointAuthSigningAlgorithm = "_",
            DefaultMaxAge = 0,
            RequireAuthTime = false,
            DefaultAcrValues = ["_"],
        };
        
        SetJsonBody(topLevelRequest);
        
        var result = await _subject.ParseRegisterRequestAsync(
            _context.Request.Body,
            _context.Request.ContentType,
            _context.RequestAborted);
        
        result.Should().NotBeNull();
        result.RequestDto.Should().BeEquivalentTo(requestDto);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task parse_when_request_and_software_statement_contain_same_metadata_should_use_software_statement_value()
    {
        var request = new RegisterRequestDTO
        {
            RedirectUris = ["https://top-level-redirect.test"],
            ClientName = "top-level-client-name",
            SoftwareStatement = CreateSoftwareStatement(new RegisterRequestDTO
            {
                RedirectUris = ["https://software-statement-redirect.test"],
                ClientName = "software-statement-client-name"
            })
        };

        SetJsonBody(request);

        var result = await _subject.ParseRegisterRequestAsync(
            _context.Request.Body,
            _context.Request.ContentType,
            _context.RequestAborted);

        result.RequestDto.RedirectUris.Should().BeEquivalentTo(["https://software-statement-redirect.test"]);
        result.RequestDto.ClientName.Should().Be("software-statement-client-name");
    }

    private void Init()
    {
        requestDto = new RegisterRequestDTO()
        {
            RedirectUris = ["https://redirect_uri.test", "https://redirect_uri2.test"],
            TokenEndpointAuthMethod = "token_endpoint_auth_method",
            GrantTypes = ["grant_type", "grant_type2"],
            ResponseTypes = ["response_type", "response_type2"],
            ClientName = "client_name",
            ClientUri = "client_uri",
            LogoUri = "logo_uri",
            Scope = "scope scope2",
            Contacts = ["contact1", "contact2"],
            TermsOfServiceUri = "terms_of_service_uri",
            PolicyUri = "policy_uri",
            JsonWebKeySetUri = "jwks_uri",
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
            UserInfoEncryptedResponseAlgorithm = "userinfo_encrypted_response_alg",
            RequestObjectSigningAlgorithm = "request_object_signing_alg",
            RequestObjectEncryptionAlgorithm = "request_object_encryption_alg",
            RequestObjectEncryption = "request_object_encryption_enc",
            TokenEndpointAuthSigningAlgorithm = "token_endpoint_auth_signing_alg",
            DefaultMaxAge = 100,
            RequireAuthTime = true,
            DefaultAcrValues = ["arc", "acr2"],
        };
        
        _context = new DefaultHttpContext();
        _options = new DynamicClientManagementOptions();
        _context.Request.Method = "POST";
        _context.Request.ContentType = "application/json";
        
        _subject = new DefaultDynamicClientManagementRequestParser(Options.Create(_options), new NullLogger<IDynamicClientManagementRequestParser>());
    }
    
    private string CreateSoftwareStatement(RegisterRequestDTO requestDto)
    {
        var requestJson = JsonSerializer.SerializeToElement(requestDto);
        
        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = "issuer",
            Claims = requestJson.EnumerateObject().ToDictionary(property => property.Name, property => (object)property.Value),
        };
        
        var handler = new JwtSecurityTokenHandler();

        return handler.WriteToken(handler.CreateJwtSecurityToken(securityTokenDescriptor));
    }
    
    private void SetJsonBody(RegisterRequestDTO requestDto)
    {
        var json = JsonSerializer.Serialize(requestDto, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        SetJsonBody(json);
    }

    private void SetJsonBody(string json)
    {
        _context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        _context.Request.ContentType = "application/json";
    }
}
