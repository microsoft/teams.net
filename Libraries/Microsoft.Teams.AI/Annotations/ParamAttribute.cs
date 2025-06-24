// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.AI.Annotations;

[AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
public class ParamAttribute(string? name = null) : Attribute
{
    /// <summary>
    /// the parameter name
    /// </summary>
    public string? Name { get; private set; } = name;

    /// <summary>
    /// the parameter description
    /// </summary>
    public string? Description { get; private set; }
}