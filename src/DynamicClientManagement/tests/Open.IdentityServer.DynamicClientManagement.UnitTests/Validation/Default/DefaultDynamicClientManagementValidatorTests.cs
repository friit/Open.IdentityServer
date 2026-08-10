// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
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
        var parseResult = CreateParseResult(new RegisterRequest());

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_client_metadata");
        result.ErrorDescription.Should().Be("RedirectUris must be provided.");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_redirect_uri_is_not_absolute_should_return_failure()
    {
        var parseResult = CreateParseResult(new RegisterRequest
        {
            RedirectUris = ["not-a-uri"]
        });

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_redirect_uri");
        result.ErrorDescription.Should().Be("RedirectUri 'not-a-uri' must be an absolute URI.");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_redirect_uri_is_not_https_should_return_failure()
    {
        var parseResult = CreateParseResult(new RegisterRequest
        {
            RedirectUris = ["http://redirect.test/callback"]
        });

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_redirect_uri");
        result.ErrorDescription.Should().Be("RedirectUri 'http://redirect.test/callback' must not use HTTP.");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_redirect_uris_are_valid_should_return_success()
    {
        var request = new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"]
        };
        var parseResult = CreateParseResult(request);

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Request.Should().BeSameAs(request);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_frontchannel_logout_session_required_is_set_without_uri_should_return_failure()
    {
        var parseResult = CreateParseResult(new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            FrontChannelLogoutSessionRequired = true
        });

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_client_metadata");
        result.ErrorDescription.Should().Be("frontchannel_logout_session_required requires FrontChannelLogoutUri to be provided.");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_frontchannel_logout_uri_is_not_absolute_should_return_failure()
    {
        var parseResult = CreateParseResult(new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            FrontChannelLogoutUri = "not-a-uri"
        });

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_client_metadata");
        result.ErrorDescription.Should().Be("FrontChannelLogoutUri 'not-a-uri' must be an absolute URI.");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_frontchannel_logout_uri_is_not_https_should_return_failure()
    {
        var parseResult = CreateParseResult(new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            FrontChannelLogoutUri = "http://logout.test/frontchannel"
        });

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_client_metadata");
        result.ErrorDescription.Should().Be("FrontChannelLogoutUri 'http://logout.test/frontchannel' must use HTTPS.");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_frontchannel_logout_uri_has_fragment_should_return_failure()
    {
        var parseResult = CreateParseResult(new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            FrontChannelLogoutUri = "https://logout.test/frontchannel#fragment"
        });

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_client_metadata");
        result.ErrorDescription.Should().Be("FrontChannelLogoutUri 'https://logout.test/frontchannel#fragment' must not contain a fragment.");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_backchannel_logout_session_required_is_set_without_uri_should_return_failure()
    {
        var parseResult = CreateParseResult(new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            BackChannelLogoutSessionRequired = true
        });

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_client_metadata");
        result.ErrorDescription.Should().Be("backchannel_logout_session_required requires BackChannelLogoutUri to be provided.");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_backchannel_logout_uri_is_not_https_should_return_failure()
    {
        var parseResult = CreateParseResult(new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            BackChannelLogoutUri = "http://logout.test/backchannel"
        });

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_client_metadata");
        result.ErrorDescription.Should().Be("BackChannelLogoutUri 'http://logout.test/backchannel' must use HTTPS.");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_logout_uris_are_valid_should_return_success()
    {
        var request = new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"],
            FrontChannelLogoutUri = "https://logout.test/frontchannel",
            FrontChannelLogoutSessionRequired = true,
            BackChannelLogoutUri = "https://logout.test/backchannel",
            BackChannelLogoutSessionRequired = true
        };
        var parseResult = CreateParseResult(request);

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Request.Should().BeSameAs(request);
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
        result.Error.Should().Be("invalid_client_metadata");
        result.ErrorDescription.Should().NotBeNullOrWhiteSpace();
        result.ErrorDescription!.Should().Contain(expectedMetadata);
        result.ErrorDescription.ToLowerInvariant().Should().Contain("unsupported");
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_signed_software_statement_is_required_should_call_signature_validator()
    {
        _options.RequireValidSoftwareStatement = true;
        var token = new JwtSecurityToken();
        var parseResult = CreateParseResult(new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"]
        }, token);

        Mock.Get(_softwareStatementValidator)
            .Setup(x => x.ValidateAsync(token, CancellationToken.None))
            .ReturnsAsync(new SoftwareStatementValidationResult(token));

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeFalse();
        Mock.Get(_softwareStatementValidator)
            .Verify(x => x.ValidateAsync(token, CancellationToken.None), Times.Once);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_signed_software_statement_is_not_required_should_not_call_signature_validator()
    {
        _options.RequireValidSoftwareStatement = false;
        var parseResult = CreateParseResult(new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"]
        }, new JwtSecurityToken());

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeFalse();
        Mock.Get(_softwareStatementValidator)
            .Verify(
                x => x.ValidateAsync(It.IsAny<JwtSecurityToken>(), It.IsAny<CancellationToken>()),
                Times.Never);
    }

    [Fact]
    [Trait("Category", Category)]
    public async Task validate_when_software_statement_signature_validation_fails_should_return_failure()
    {
        _options.RequireValidSoftwareStatement = true;
        var token = new JwtSecurityToken();
        var parseResult = CreateParseResult(new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"]
        }, token);

        Mock.Get(_softwareStatementValidator)
            .Setup(x => x.ValidateAsync(token, CancellationToken.None))
            .ReturnsAsync(new SoftwareStatementValidationResult(token, "invalid_software_statement", "Software Statement signature validation failed."));

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_software_statement");
        result.ErrorDescription.Should().Be("Software Statement signature validation failed.");
    }

    [Fact]
    public async Task validate_when_software_statement_is_missing_issuer_should_return_failure()
    {
        _options.RequireValidSoftwareStatement = true;
        var token = new JwtSecurityToken();
        Mock.Get(_softwareStatementValidator)
            .Setup(x => x.ValidateAsync(token, CancellationToken.None))
            .ReturnsAsync(new SoftwareStatementValidationResult(token, "invalid_software_statement", "Software Statement issuer is missing."));

        var parseResult = CreateParseResult(new RegisterRequest
        {
            RedirectUris = ["https://redirect.test/callback"]
        }, token);

        var result = await _subject.ValidateAsync(parseResult, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be("invalid_software_statement");
        result.ErrorDescription.Should().Be("Software Statement issuer is missing.");
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
                JsonWebKeySetUri = "https://client.test/jwks"
            },
            "jwks_uri"
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
                SectorIdentifierUri = "https://client.test/sector"
            },
            "sector_identifier_uri"
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
}
