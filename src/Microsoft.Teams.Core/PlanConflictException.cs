// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;

namespace Microsoft.Teams.Core;

/// <summary>Teams rejected a Plan operation with HTTP 409 and error code PlanConflict. Do not retry blindly.</summary>
[Experimental("ExperimentalTeamsPlan")]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors",
    Justification = "Created only from a verified service conflict, retaining its HTTP failure details.")]
public sealed class PlanConflictException : HttpRequestException
{
    /// <summary>The service error code.</summary>
    public string ErrorCode { get; } = "PlanConflict";

    /// <summary>The service's error message, when supplied as a string.</summary>
    public string? ErrorMessage { get; }

    /// <summary>The unmodified response body. The original HTTP exception is available as InnerException.</summary>
    public string ResponseBody { get; }

    private PlanConflictException(HttpRequestException error, string responseBody, string? errorMessage)
        : base(error.Message, error, error.StatusCode)
    {
        ResponseBody = responseBody;
        ErrorMessage = errorMessage;
    }

    internal static bool TryCreate(HttpRequestException error, string responseBody, [NotNullWhen(true)] out PlanConflictException? conflict)
    {
        conflict = null;
        if (error.StatusCode != HttpStatusCode.Conflict)
            return false;

        try
        {
            using JsonDocument document = JsonDocument.Parse(responseBody);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("error", out JsonElement serviceError)
                || serviceError.ValueKind != JsonValueKind.Object
                || !serviceError.TryGetProperty("code", out JsonElement code)
                || code.ValueKind != JsonValueKind.String
                || code.GetString() != "PlanConflict")
                return false;

            string? message = serviceError.TryGetProperty("message", out JsonElement value)
                && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
            conflict = new PlanConflictException(error, responseBody, message);
            return true;
        }
        catch (JsonException)
        {
            // An unrecognized response must keep propagating as the original HTTP failure.
            return false;
        }
    }
}
