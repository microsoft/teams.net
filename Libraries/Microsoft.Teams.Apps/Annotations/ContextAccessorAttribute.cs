// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Reflection;

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps.Annotations;

[AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
public abstract class ContextAccessorAttribute : Attribute
{
    public abstract object? GetValue(IContext<IActivity> context, ParameterInfo parameter);
}