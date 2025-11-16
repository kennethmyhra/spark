/* 
 * Copyright (c) 2015-2018, Firely <info@fire.ly>
 * Copyright (c) 2021-2025, Incendi <info@incendi.no>
 * 
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Net;
using Spark.Engine.Extensions;

namespace Spark.Engine.Core;

// This class serves instances of "Response"
public static class Respond
{
    public static FhirResponse WithError(HttpStatusCode code)
    {
        return new FhirResponse(code, Key.Null, null);
    }

    public static FhirResponse WithCode(HttpStatusCode code)
    {
        return new FhirResponse(code);
    }

    public static FhirResponse WithCode(int code)
    {
        return new FhirResponse((HttpStatusCode)code);
    }

    public static FhirResponse WithError(HttpStatusCode code, string message, params object[] args)
    {
        OperationOutcome outcome = new();
        outcome.AddError(string.Format(message, args));
        return new FhirResponse(code, outcome);
    }

    public static FhirResponse WithResource(
        int code,
        Resource resource,
        ReturnPreference returnPreference = ReturnPreference.Representation)
    {
        return new FhirResponse((HttpStatusCode)code, resource,  returnPreference);
    }

    public static FhirResponse WithResource(
        Resource resource,
        ReturnPreference returnPreference = ReturnPreference.Representation)
    {
        return new FhirResponse(HttpStatusCode.OK, resource);
    }

    public static FhirResponse WithResource(
        Key key,
        Resource resource,
        ReturnPreference returnPreference = ReturnPreference.Representation)
    {
        return new FhirResponse(HttpStatusCode.OK, key, resource,  returnPreference);
    }

    public static FhirResponse WithResource(
        HttpStatusCode code,
        Key key, Resource resource,
        ReturnPreference returnPreference = ReturnPreference.Representation)
    {
        return new FhirResponse(code, key, resource, returnPreference);
    }

    public static FhirResponse WithEntry(
        HttpStatusCode code,
        Entry entry,
        ReturnPreference returnPreference = ReturnPreference.Representation)
    {

        return new FhirResponse(code, entry.Key, entry.Resource, returnPreference);
    }

    public static FhirResponse WithBundle(Bundle bundle)
    {
        return new FhirResponse(HttpStatusCode.OK, bundle);
    }

    public static FhirResponse WithBundle(IEnumerable<Entry> entries, Uri serviceBase)
    {
        Bundle bundle = new();
        bundle.Append(entries);
        return WithBundle(bundle);
    }

    public static FhirResponse WithMeta(Meta meta)
    {
        Parameters parameters = new() { { nameof(Meta), meta } };
        return WithResource(parameters);
    }

    public static FhirResponse WithMeta(Entry entry)
    {
        return entry.Resource is { Meta: not null }
            ? WithMeta(entry.Resource.Meta)
            : WithError(HttpStatusCode.InternalServerError, "Could not retrieve meta. Meta was not present on the resource");
    }

    public static FhirResponse WithKey(HttpStatusCode code, IKey key)
    {
        return new FhirResponse(code, key, null);
    }

    public static FhirResponse WithResource(HttpStatusCode code, Entry entry)
    {
        return new FhirResponse(code, entry.Key, entry.Resource, entry.ReturnPreference);
    }

    public static FhirResponse WithResource(Entry entry)
    {
        return new FhirResponse(HttpStatusCode.OK, entry.Key, entry.Resource, entry.ReturnPreference);
    }

    public static FhirResponse NotFound(IKey key)
    {
        return key.VersionId == null
            ? WithError(HttpStatusCode.NotFound, "No {0} resource with id {1} was found.", key.TypeName, key.ResourceId)
            : WithError(HttpStatusCode.NotFound, "There is no {0} resource with id {1}, or there is no version {2}", key.TypeName, key.ResourceId, key.VersionId);
        // For security reasons (leakage): keep message in sync with Error.NotFound(key)
    }

    public static FhirResponse Gone(Entry entry)
    {

        var message = string.Format(
            "A {0} resource with id {1} existed, but was deleted on {2} (version {3}).",
            entry.Key.TypeName,
            entry.Key.ResourceId,
            entry.When,
            entry.Key.ToRelativeUri());

        return WithError(HttpStatusCode.Gone, message);
    }

    public static FhirResponse NotImplemented
    {
        get
        {
            return WithError(HttpStatusCode.NotImplemented);
        }
    }

    public static FhirResponse Success
    {
        get
        {
            return new FhirResponse(HttpStatusCode.OK);
        }
    }
}
