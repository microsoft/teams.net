// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance",
    "CA1873:Avoid potentially expensive logging",
    Justification = "<Pending>",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Teams.Bot.Core")]

[assembly: SuppressMessage("Performance",
    "CA1848:Use the LoggerMessage delegates",
    Justification = "<Pending>",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Teams.Bot.Core")]

[assembly: SuppressMessage("Design",
    "CA1054:URI-like parameters should not be strings",
    Justification = "String URLs are used for consistency with existing API patterns",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Teams.Bot.Core.Http")]

[assembly: SuppressMessage("Usage",
    "CA2227:Collection properties should be read only",
    Justification = "<Pending>",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Teams.Bot.Core")]
