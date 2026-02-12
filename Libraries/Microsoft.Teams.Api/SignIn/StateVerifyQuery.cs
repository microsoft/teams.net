// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.SignIn;

/// <summary>
/// Signin state (part of signin action auth flow) verification invoke query
/// </summary>
public class StateVerifyQuery
{
    /// <summary>
    /// The state value originally received when the
    /// signin web flow is finished with a state posted back to client via tab SDK
    /// microsoftTeams.authentication.notifySuccess(state).
    /// Can be either a string or a JSON object depending on the platform (Android/iOS may send objects).
    /// </summary>
    [JsonPropertyName("state")]
    [JsonPropertyOrder(0)]
    public JsonElement? State { get; set; }

    /// <summary>
    /// Gets the state as a string if it is a string value, otherwise returns the JSON representation.
    /// </summary>
    /// <returns>The state as a string, or null if State is null.</returns>
    public string? GetStateAsString()
    {
        if (State == null)
        {
            return null;
        }

        var element = State.Value;
        
        // If it's a string, return the string value
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }

        // Otherwise, return the JSON representation
        return element.ToString();
    }

    /// <summary>
    /// Tries to get the state as a string value.
    /// </summary>
    /// <param name="stateString">The state as a string if it is a string value.</param>
    /// <returns>True if the state is a string value, false otherwise.</returns>
    public bool TryGetStateAsString(out string? stateString)
    {
        stateString = null;

        if (State == null)
        {
            return false;
        }

        var element = State.Value;

        if (element.ValueKind == JsonValueKind.String)
        {
            stateString = element.GetString();
            return true;
        }

        return false;
    }
}