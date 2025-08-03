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

namespace Spark.Engine.Core;

public enum EntryState { Internal, Undefined, External }

public class Entry
{
    private IKey _key;
    private DateTimeOffset? _when;

    public IKey Key {
        get
        {
            if (Resource != null && !(Method == Bundle.HTTPVerb.PATCH && Resource is Parameters))
            {
                return Resource.ExtractKey();
            }
            else
            {
                return _key;
            }
        }
        set
        {
            if (Resource != null)
            {
                value.ApplyTo(Resource);
            }
            else
            {
                _key = value;
            }
        }
    }

    public Resource Resource { get; set; }

    public Bundle.HTTPVerb Method { get; set; }

    // API: HttpVerb should not be in Bundle.
    public DateTimeOffset? When
    {
        get
        {
            if (Resource != null && Resource.Meta != null)
            {
                return Resource.Meta.LastUpdated;
            }
            else
            {
                return _when;
            }
        }
        set
        {
            if (Resource != null)
            {
                if (Resource.Meta == null) Resource.Meta = new Meta();
                Resource.Meta.LastUpdated = value?.TruncateToMillis();
            }
            else
            {
                _when = value;
            }
        }
    }

    public EntryState State { get; set; }

    public ReturnPreference ReturnPreference { get; set; }

    protected Entry(Bundle.HTTPVerb method, IKey key, DateTimeOffset? when, Resource resource,
        ReturnPreference returnPreference = ReturnPreference.Representation)
    {
        if (resource != null && !(method == Bundle.HTTPVerb.PATCH && resource is Parameters))
        {
            key.ApplyTo(resource);
        }
        else
        {
            Key = key;
        }
        Resource = resource;
        ReturnPreference = returnPreference;
        Method = method;
        When = when ?? DateTimeOffset.Now;
        State = EntryState.Undefined;
    }

    protected Entry(IKey key, Resource resource)
    {
        Key = key;
        Resource = resource;
        State = EntryState.Undefined;
    }

    public static Entry Create(Bundle.HTTPVerb method, Resource resource,
        ReturnPreference returnPreference = ReturnPreference.Representation) =>
        new(method, null, null, resource, returnPreference);

    public static Entry Create(Bundle.HTTPVerb method, IKey key,
        ReturnPreference returnPreference = ReturnPreference.Representation) =>
        new(method, key, null, null, returnPreference);

    public static Entry Create(Bundle.HTTPVerb method, IKey key, Resource resource,
        ReturnPreference returnPreference = ReturnPreference.Representation) =>
        new(method, key, null, resource, returnPreference);

    public static Entry Create(Bundle.HTTPVerb method, IKey key, DateTimeOffset when,
        ReturnPreference returnPreference = ReturnPreference.Representation) =>
        new(method, key, when, null, returnPreference);

    /// <summary>
    ///  Creates a deleted entry
    /// </summary>
    public static Entry DELETE(IKey key, DateTimeOffset? when)
    {
        return Create(Bundle.HTTPVerb.DELETE, key, DateTimeOffset.UtcNow);
    }

    public bool IsDelete
    {
        get
        {
            return Method == Bundle.HTTPVerb.DELETE;
        }
        set
        {
            Method = Bundle.HTTPVerb.DELETE;
            Resource = null;
        }
    }

    public bool IsPresent
    {
        get
        {
            return Method != Bundle.HTTPVerb.DELETE;
        }
    }

    public static Entry POST(
        IKey key,
        Resource resource,
        ReturnPreference returnPreference = ReturnPreference.Representation) =>
        Create(Bundle.HTTPVerb.POST, key, resource, returnPreference);

    public static Entry POST(Resource resource, ReturnPreference returnPreference = ReturnPreference.Representation) => Create(Bundle.HTTPVerb.POST, resource, returnPreference);

    public static Entry PUT(
        IKey key,
        Resource resource,
        ReturnPreference returnPreference = ReturnPreference.Representation) =>
        Create(Bundle.HTTPVerb.PUT, key, resource, returnPreference);

    public static Entry PATCH(
        IKey key,
        Resource resource,
        ReturnPreference returnPreference = ReturnPreference.Representation) =>
        Create(Bundle.HTTPVerb.PATCH, key, resource, returnPreference);

    public override string ToString() => $"{Method} {Key}";
}
