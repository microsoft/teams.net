// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Apps.Plugins;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
public class DependencyAttribute : Attribute
{
    public string? Name { get; set; }
    public bool Optional { get; set; } = false;

    public DependencyAttribute() : base()
    {

    }

    public DependencyAttribute(string name) : base()
    {
        Name = name;
    }

    public DependencyAttribute(string name, bool optional) : base()
    {
        Name = name;
        Optional = optional;
    }
}