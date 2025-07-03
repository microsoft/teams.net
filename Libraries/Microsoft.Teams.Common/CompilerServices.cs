﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;

namespace System.Runtime.CompilerServices;

public class RequiredMemberAttribute : Attribute { }
public class CompilerFeatureRequiredAttribute : Attribute
{
    public CompilerFeatureRequiredAttribute(string name) { }
}

/// <summary>
/// Reserved to be used by the compiler for tracking metadata.
/// This class should not be used by developers in source code.
/// This dummy class is required to compile records when targeting .NET Standard
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IsExternalInit
{
}