// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.Annotations;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
[Obsolete("Use Minimal APIs instead.")]
public class TeamsControllerAttribute(string? name = null) : Attribute
{
    public string? Name { get; } = name;
}