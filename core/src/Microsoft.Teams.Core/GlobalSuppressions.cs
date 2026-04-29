// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage",
    "CA2227:Collection properties should be read only",
    Justification = "Required for serialization",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Teams.Core")]
