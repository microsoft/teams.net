﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Teams.Common.Extensions;

public static class TypeExtensions
{
    public static bool IsAssignableTo(this Type type, Type? targetType) => targetType?.IsAssignableFrom(type) ?? false;
}