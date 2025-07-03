// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities;

public partial class ActivityType : StringEnum
{
    public static readonly ActivityType InstallUpdate = new("installationUpdate");
    public bool IsInstallUpdate => InstallUpdate.Equals(Value);
}

public class InstallUpdateActivity() : Activity(ActivityType.InstallUpdate)
{
    [JsonPropertyName("action")]
    [JsonPropertyOrder(31)]
    public required InstallUpdateAction Action { get; set; }

    public override string GetPath()
    {
        return string.Join(".", ["Activity", Type.ToPrettyString(), Action.ToPrettyString()]);
    }
}

[JsonConverter(typeof(JsonConverter<InstallUpdateAction>))]
public class InstallUpdateAction(string value) : StringEnum(value)
{
    public static readonly InstallUpdateAction Add = new("add");
    public bool IsAdd => Add.Equals(Value);

    public static readonly InstallUpdateAction Remove = new("remove");
    public bool IsRemove => Remove.Equals(Value);

    public string ToPrettyString()
    {
        var value = ToString();
        return $"{value.First().ToString().ToUpper()}{value.AsSpan(1).ToString()}";
    }
}