// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage",
    "CA2227:Collection properties should be read only",
    Justification = "Required for serialization",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Teams.Core")]

[assembly: SuppressMessage("Design",
    "CA1054:URI-like parameters should not be strings",
    Justification = "Callers build interpolated URLs with query strings and Uri-escaped segments; string parameters are the natural shape for this HTTP plumbing.",
    Scope = "type",
    Target = "~T:Microsoft.Teams.Core.Http.BotHttpClient")]
