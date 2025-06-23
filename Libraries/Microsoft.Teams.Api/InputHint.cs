// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api;

[JsonConverter(typeof(JsonConverter<InputHint>))]
public class InputHint(string value) : StringEnum(value)
{
    public static readonly InputHint AcceptingInput = new("acceptingInput");
    public bool IsAcceptingInput => AcceptingInput.Equals(Value);

    public static readonly InputHint IgnoringInput = new("ignoringInput");
    public bool IsIgnoringInput => IgnoringInput.Equals(Value);

    public static readonly InputHint ExpectingInput = new("expectingInput");
    public bool IsExpectingInput => ExpectingInput.Equals(Value);
}