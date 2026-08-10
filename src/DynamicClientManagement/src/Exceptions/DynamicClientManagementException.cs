// Copyright (c) 2026, Rock Solid Knowledge Ltd
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Net;

namespace Open.IdentityServer.DynamicClientManagement.Exceptions;

public abstract class DynamicClientManagementException : Exception
{
    protected DynamicClientManagementException(
        string error,
        string? errorDescription = null,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        Exception? innerException = null)
        : base(errorDescription ?? error, innerException)
    {
        Error = error;
        ErrorDescription = errorDescription;
        StatusCode = statusCode;
    }

    public string Error { get; }
    public string? ErrorDescription { get; }
    public HttpStatusCode StatusCode { get; }
}