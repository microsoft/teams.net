// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Common.Http;

public class HttpException : Exception
{
    public required HttpResponseHeaders Headers { get; set; }
    public required HttpStatusCode StatusCode { get; set; }
    public HttpRequestMessage? Request { get; set; }
    public object? Body { get; set; }

    public override string ToString()
    {
        if (Body is string textBody)
        {
            return textBody;
        }

        return JsonSerializer.Serialize(Body, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}