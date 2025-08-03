/*
 * Copyright (c) 2025, Incendi <info@incendi.no>
 *
 * SPDX-License-Identifier: BSD-3-Clause
 */

using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Http;
using Spark.Engine.Extensions;
using Spark.Engine.Test.Mocks;
using Xunit;

namespace Spark.Engine.Test.Extensions;

public class HttpRequestExtensionsTests
{
    [Fact]
    public void PreferHeaderNotSet_Returns_ReturnPreference_Representation()
    {
        HttpRequestMock request = new();

        ReturnPreference preferHeaderValue = request.GetPreferHeaderValue();

        Assert.Equal(ReturnPreference.Representation, preferHeaderValue);
    }

    [Fact]
    public void PreferHeaderSetToRepresentationReturns_ReturnPreference_Representation()
    {
        HttpRequestMock request = new();
        request.Headers.Append("Prefer", "return=representation");

        ReturnPreference returnPreference = request.GetPreferHeaderValue();

        Assert.Equal(ReturnPreference.Representation, returnPreference);
    }

    [Fact]
    public void PreferHeaderSetToMinimal_Returns_ReturnPreference_Minimal()
    {
        HttpRequestMock request = new();
        request.Headers.Append("Prefer", "return=minimal");

        ReturnPreference preferHeaderValue = request.GetPreferHeaderValue();

        Assert.Equal(ReturnPreference.Minimal, preferHeaderValue);
    }

    [Fact]
    public void PreferHeaderSetToOperationOutcome_Returns_ReturnPreference_OperationOutcome()
    {
        HttpRequestMock request = new();
        request.Headers.Append("Prefer", "return=OperationOutcome");

        ReturnPreference preferHeaderValue = request.GetPreferHeaderValue();

        Assert.Equal(ReturnPreference.OperationOutcome, preferHeaderValue);
    }

    [Fact]
    public void PreferHeaderSetToOperationOutcomeInLowerCase_Returns_ReturnPreference_OperationOutcome()
    {
        HttpRequestMock request = new();
        request.Headers.Append("Prefer", "return=operationoutcome");

        ReturnPreference preferHeaderValue = request.GetPreferHeaderValue();

        Assert.Equal(ReturnPreference.OperationOutcome, preferHeaderValue);
    }
}
