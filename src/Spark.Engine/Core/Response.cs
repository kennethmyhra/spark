/*
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Spark.Engine.Extensions;
using System;
using System.Collections.Generic;
using System.Net;

namespace Spark.Engine.Core;
// THe response class is an abstraction of the Fhir REST responses
// This way, it's easier to implement multiple WebApi controllers
// without having to implement functionality twice.
// The FhirService always responds with a "Response"

public abstract class FhirResponseBase
{
    private static readonly Dictionary<HttpStatusCode, string> DIAGNOSTIC_TEXTS = new()
    {
        { HttpStatusCode.OK, "Successfully updated resource \"{resource}\"" },
        { HttpStatusCode.Created, "Successfully created resource \"{resource}\"" }
    };

    protected Resource _resource;

    protected static string BuildDiagnosticText(HttpStatusCode statusCode, IKey key)
    {
        if (key == null)
            return null;

        string relativeUrl = $"{key.TypeName}/{key.ResourceId}";
        if (key.HasVersionId())
            relativeUrl += $"/_history/{key.VersionId}";

        return DIAGNOSTIC_TEXTS.TryGetValue(statusCode, out string value)
            ? value.Replace("{resource}", relativeUrl)
            : null;
    }

    public HttpStatusCode StatusCode { get; protected init; }

    public IKey Key { get; protected init; }

    public ReturnPreference ReturnPreference { get; protected init; } =  ReturnPreference.Representation;

    public bool IsValid
    {
        get
        {
            int code = (int)StatusCode;
            return code <= 300;
        }
    }

    public bool HasBody => _resource != null;

    public override string ToString()
    {
        string details = _resource != null ? $"({_resource.TypeName})" : null;
        string location = Key?.ToString();
        return $"{(int)StatusCode}: {StatusCode.ToString()} {details} ({location})";
    }
}

public class FhirResponse<T> : FhirResponseBase where T : Resource
{
    public FhirResponse(HttpStatusCode code, IKey key, T resource)
    {
        StatusCode = code;
        Key = key;
        Resource = resource;
    }

    public FhirResponse(HttpStatusCode code, T resource)
    {
        StatusCode = code;
        Key = null;
        Resource = resource;
    }

    public FhirResponse(HttpStatusCode code)
    {
        StatusCode = code;
    }

    public T Resource
    {
        get => _resource as T;
        private init => _resource = value;
    }
}

public class FhirResponse : FhirResponseBase
{
    public FhirResponse(
        HttpStatusCode code,
        IKey key,
        Resource resource,
        ReturnPreference returnPreference = ReturnPreference.Representation)
    {
        StatusCode = code;
        Key = key;
        Resource = resource;
        ReturnPreference = returnPreference;
    }

    public FhirResponse(
        HttpStatusCode code,
        Resource resource,
        ReturnPreference returnPreference = ReturnPreference.Representation)
    {
        StatusCode = code;
        Key = null;
        Resource = resource;
        ReturnPreference = returnPreference;
    }

    public FhirResponse(HttpStatusCode code)
    {
        StatusCode = code;
        Key = null;
        Resource = null;
    }

    public Resource Resource
    {
        get
        {
            if (_resource == null)
                return null;

            return ReturnPreference switch
            {
                ReturnPreference.Representation => _resource,
                ReturnPreference.Minimal => null,
                ReturnPreference.OperationOutcome => new OperationOutcome
                {
                    Issue =
                    [
                        new OperationOutcome.IssueComponent
                        {
                            Severity = OperationOutcome.IssueSeverity.Information,
                            Code = OperationOutcome.IssueType.Informational,
                            Diagnostics = BuildDiagnosticText(StatusCode, Key)
                        }
                    ]
                },
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        internal set => _resource = value;
    }
}
