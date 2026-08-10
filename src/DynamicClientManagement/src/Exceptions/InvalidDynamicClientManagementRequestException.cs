// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Net;

namespace Open.IdentityServer.DynamicClientManagement.Exceptions;

public class InvalidDynamicClientManagementRequestException : DynamicClientManagementException
{
    public InvalidDynamicClientManagementRequestException(string error, string? errorDescription = null, HttpStatusCode statusCode = HttpStatusCode.BadRequest, Exception? innerException = null) : base(error, errorDescription, statusCode, innerException)
    {
    }
}