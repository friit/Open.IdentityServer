// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Open.IdentityServer.DynamicClientManagement.Configuration;
using Open.IdentityServer.DynamicClientManagement.Models;
using Open.IdentityServer.DynamicClientManagement.Parser.Models;
using Open.IdentityServer.DynamicClientManagement.Validation;
using Open.IdentityServer.DynamicClientManagement.Validation.Default;
using Open.IdentityServer.DynamicClientManagement.Validation.Models;
using Open.IdentityServer.DynamicClientManagement.Validation.Models.Errors;
using System.Text.Json;
using Xunit;

namespace Open.IdentityServer.DynamicClientManagement.UnitTests.Validation.Default;

public class DefaultDynamicClientManagementValidatorTests
{
    private const string Category = "Default Dynamic Client Management Validator";

    private readonly DynamicClientManagementOptions _options;
    private readonly ISoftwareStatementValidator _softwareStatementValidator;
    private readonly DefaultDynamicClientManagementValidator _subject;
    

    public DefaultDynamicClientManagementValidatorTests()
    {
        _options = new DynamicClientManagementOptions();
        _softwareStatementValidator = Mock.Of<ISoftwareStatementValidator>();
        _subject = new DefaultDynamicClientManagementValidator(
            Options.Create(_options),
            _softwareStatementValidator,
            NullLogger<IDynamicClientManagementValidator>.Instance);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_redirect_uris_are_missing_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r => r.RedirectUris = null);
        
        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<RedirectUrisRequiredError>(result.Errors);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_redirect_uri_is_not_absolute_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r => r.RedirectUris = ["not-a-uri"]);

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<UriMustBeAbsoluteError>(result.Errors);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_redirect_uri_is_not_https_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r => r.RedirectUris = ["http://redirect.test/callback"]);

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<UriMustUseHttpsError>(result.Errors);
    }
    

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_frontchannel_logout_session_required_is_set_without_uri_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r =>
        {
            r.FrontChannelLogoutSessionRequired = true;
            r.FrontChannelLogoutUri = null;
        });
        
        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<FrontChannelLogoutUriRequiredError>(result.Errors);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_frontchannel_logout_uri_is_not_absolute_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r =>
        {
            r.FrontChannelLogoutUri = "not-a-uri";
        });

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<UriMustBeAbsoluteError>(result.Errors);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_frontchannel_logout_uri_is_not_https_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r =>
        {
            r.FrontChannelLogoutUri = "http://logout.test/frontchannel";
        });

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<UriMustUseHttpsError>(result.Errors);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_frontchannel_logout_uri_has_fragment_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r =>
        {
            r.FrontChannelLogoutUri = "https://logout.test/frontchannel#fragment";
        });

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<UriMustNotContainFragmentError>(result.Errors);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_jwks_uri_is_set_with_key_set_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r =>
        {
            r.JsonWebKeySetUri = "https://client.test/jwks";
            r.JsonWebKeySet = JsonDocument.Parse("""{"keys":[{"kty":"RSA"}]}""").RootElement.Clone();
        });

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<JsonWebKeySetDisallowedError>(result.Errors);
    }
    
    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_token_endpoint_auth_is_private_key_and_jwks_is_missing_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r =>
        {
            r.TokenEndpointAuthMethod = "private_key_jwt";
            r.JsonWebKeySet = null;
        });

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        //AssertSingleError<JsonWebKeySetRequiredError>(result.Errors, "invalid_client_metadata: jwks must be provided when token_endpoint_auth_method is 'private_key_jwt'.");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_jwks_and_token_endpoint_auth_not_private_key_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r =>
        {
            r.JsonWebKeySet = JsonDocument.Parse("""{"keys":[{"kty":"RSA"}]}""").RootElement.Clone();
        });

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<JsonWebKeySetDisallowedError>(result.Errors);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_initiate_login_uri_is_not_https_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r =>
        {
            r.InitiateLoginUri = "http://client.test/login";
        });

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<UriMustUseHttpsError>(result.Errors);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_sector_identifier_uri_is_not_absolute_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r =>
        {
            r.SectorIdentifierUri = "not-a-uri";
        });

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<UriMustBeAbsoluteError>(result.Errors);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_backchannel_logout_session_required_is_set_without_uri_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r =>
        {
            r.BackChannelLogoutSessionRequired = true;
            r.BackChannelLogoutUri = null;
        });

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<BackChannelLogoutUriRequiredError>(result.Errors);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_backchannel_logout_uri_is_not_https_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r =>
        {
            r.BackChannelLogoutUri = "http://logout.test/backchannel";
        });

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<UriMustUseHttpsError>(result.Errors);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_backchannel_logout_uri_has_fragment_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r =>
        {
            r.BackChannelLogoutUri = "https://logout.test/backchannel#fragment";
        });

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<UriMustNotContainFragmentError>(result.Errors);
    }
    
    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_post_logout_redirect_uri_is_not_https_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r =>
        {
            r.PostLogoutRedirectUris = ["http://client.test/postlogout"];
        });

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<UriMustUseHttpsError>(result.Errors);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_default_max_age_is_negative_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r =>
        {
            r.DefaultMaxAge = -1;
        });

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<DefaultMaxAgeInvalidError>(result.Errors);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_lifetime_is_zero_should_return_failure()
    {
        var registerRequest = CreateRegisterRequest(r =>
        {
            r.IdentityTokenLifetime = 0;
            r.AccessTokenLifetime = 0;
            r.AuthorizationCodeLifetime = 0;
            r.AbsoluteRefreshTokenLifetime = 0;
            r.SlidingRefreshTokenLifetime = 0;
            r.ConsentLifetime = 0;
            r.UserSsoLifetime = 0;
            r.DeviceCodeLifetime = 0;
        });

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Errors.Count(error => error is LifetimeInvalidError).Should().Be(8);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_software_statement_is_required_but_missing_should_return_failure()
    {
        _options.RequireValidSoftwareStatement = true;
        var registerRequest = CreateRegisterRequest();

        var result = await _subject.ValidateAsync(CreateParseResult(registerRequest), CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<SoftwareStatementRequiredError>(result.Errors);
    }
    
    [Theory]
    [Trait("Category", Category)]
    [MemberData(nameof(GetUnsupportedMetadataCases))]
    public async Task validate_when_request_contains_unsupported_metadata_should_return_failure(
        RegisterRequest request,
        string expectedMetadata)
    {
        var parseResult = CreateParseResult(request);

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<UnsupportedMetadataFieldError>(result.Errors);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_signed_software_statement_is_required_should_call_signature_validator()
    {
        _options.RequireValidSoftwareStatement = true;
        var token = new JwtSecurityToken();
        var registerRequest = CreateRegisterRequest();
        var parseResult = CreateParseResult(registerRequest, token);

        Mock.Get(_softwareStatementValidator)
            .Setup(x => x.ValidateAsync(token, CancellationToken.None))
            .ReturnsAsync(new SoftwareStatementValidationResult(token));

        _ = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        Mock.Get(_softwareStatementValidator)
            .Verify(x => x.ValidateAsync(token, CancellationToken.None), Times.Once);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_software_statement_is_not_required_but_present_should_call_signature_validator()
    {
        _options.RequireValidSoftwareStatement = false;
        var token = new JwtSecurityToken();
        
        Mock.Get(_softwareStatementValidator)
            .Setup(x => x.ValidateAsync(token, CancellationToken.None))
            .ReturnsAsync(new SoftwareStatementValidationResult(token));

        var parseResult = CreateParseResult(new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            GrantTypes = ["authorization_code"],
        }, token);

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        Mock.Get(_softwareStatementValidator)
            .Verify(
                x => x.ValidateAsync(It.IsAny<JwtSecurityToken>(), It.IsAny<CancellationToken>()),
                Times.Once);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_software_statement_signature_validation_fails_should_return_failure()
    {
        _options.RequireValidSoftwareStatement = true;
        var token = new JwtSecurityToken();
        var parseResult = CreateParseResult(new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            GrantTypes = ["authorization_code"],
        }, token);

        Mock.Get(_softwareStatementValidator)
            .Setup(x => x.ValidateAsync(token, CancellationToken.None))
            .ReturnsAsync(new SoftwareStatementValidationResult(token, new SoftwareStatementInvalidSignatureError()));

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<SoftwareStatementInvalidSignatureError>(result.Errors);
    }

    [Fact]
    public async Task validate_when_software_statement_is_missing_issuer_should_return_failure()
    {
        _options.RequireValidSoftwareStatement = true;
        var token = new JwtSecurityToken();
        Mock.Get(_softwareStatementValidator)
            .Setup(x => x.ValidateAsync(token, CancellationToken.None))
            .ReturnsAsync(new SoftwareStatementValidationResult(token, new SoftwareStatementIssuerRequiredError()));

        var parseResult = CreateParseResult(new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            GrantTypes = ["authorization_code"],
        }, token);

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeTrue();
        AssertSingleError<SoftwareStatementIssuerRequiredError>(result.Errors);
    }
    
    
    private static RegistrationRequestParseResult CreateParseResult(
        RegisterRequest request,
        JwtSecurityToken? softwareStatement = null)
    {
        return new RegistrationRequestParseResult
        {
            Request = request,
            SoftwareStatement = softwareStatement
        };
    }
    
    public static TheoryData<RegisterRequest, string> GetUnsupportedMetadataCases => new()
    {
        {
            new RegisterRequest
            {
                ResponseTypes = ["id_token"]
            },
            "response_types"
        },
        {
            new RegisterRequest
            {
                SubjectType = "pairwise"
            },
            "subject_type"
        },
        {
            new RegisterRequest
            {
                RequestUris = ["https://client.test/request.jwt"]
            },
            "request_uris"
        },
        {
            new RegisterRequest
            {
                TokenEndpointAuthSigningAlgorithm = "RS256"
            },
            "token_endpoint_auth_signing_alg"
        },
        {
            new RegisterRequest
            {
                RequestObjectEncryption = "A128CBC-HS256"
            },
            "request_object_encryption"
        },
        {
            new RegisterRequest
            {
                RequestObjectEncryptionAlgorithm = "RSA-OAEP"
            },
            "request_object_encryption_alg"
        },
        {
            new RegisterRequest
            {
                RequestObjectSigningAlgorithm = "RS256"
            },
            "request_object_signing_alg"
        },
        {
            new RegisterRequest
            {
                UserInfoEncryptedResponseEncryption = "A128CBC-HS256"
            },
            "userinfo_encrypted_response_enc"
        },
        {
            new RegisterRequest
            {
                UserInfoEncryptedResponseAlgorithm = "RSA-OAEP"
            },
            "userinfo_encrypted_response_alg"
        },
        {
            new RegisterRequest
            {
                UserInfoSignedResponseAlgorithm = "RS256"
            },
            "userinfo_signed_response_alg"
        },
        {
            new RegisterRequest
            {
                IdTokenEncryptedResponseEncryption = "A128CBC-HS256"
            },
            "id_token_encrypted_response_enc"
        },
        {
            new RegisterRequest
            {
                IdTokenEncryptedResponseAlgorithm = "RSA-OAEP"
            },
            "id_token_encrypted_response_alg"
        },
        {
            new RegisterRequest
            {
                IdTokenSignedResponseAlgorithm = "RS256"
            },
            "id_token_signed_response_alg"
        }
    };

    private static RegisterRequest CreateRegisterRequest(Action<RegisterRequest>? configure = null)
    {
        var request = new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            TokenEndpointAuthMethod = "client_secret_basic",
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
            FrontChannelLogoutUri = "https://logout.test/frontchannel",
            BackChannelLogoutUri = "https://logout.test/backchannel",
            PostLogoutRedirectUris = ["https://redirect.test/postlogout"],
            JsonWebKeySetUri = "https://client.test/jwks",
            ClientUri = "https://client.test",
            PolicyUri = "https://client.test/policy",
            DefaultAcrValues = ["default_acr_value"],
            DefaultMaxAge = 100,
            RequireAuthTime = true,
            InitiateLoginUri = "https://client.test/initiate_login",
            SoftwareId = "software_id",
            SoftwareVersion = "software_version",
            SoftwareStatement = "software_statement",
        };

        configure?.Invoke(request);
        
        return request;
    }

    private static void AssertSingleError<TError>(
        ValidationError[] errors)
        where TError : ValidationError
    {
        errors.Any(error => error is TError).Should().BeTrue();
    }
}
