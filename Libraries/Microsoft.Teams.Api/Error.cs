// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api;

public class Error
{
    [JsonPropertyName("code")]
    [JsonPropertyOrder(0)]
    public required string Code { get; set; }

    [JsonPropertyName("message")]
    [JsonPropertyOrder(1)]
    public required string Message { get; set; }

    [JsonPropertyName("innerHttpError")]
    [JsonPropertyOrder(2)]
    public InnerHttpError? InnerHttpError { get; set; }
}

public class InnerHttpError
{
    [JsonPropertyName("statusCode")]
    [JsonPropertyOrder(0)]
    public int? StatusCode { get; set; }

    [JsonPropertyName("body")]
    [JsonPropertyOrder(1)]
    public object? Body { get; set; }
}