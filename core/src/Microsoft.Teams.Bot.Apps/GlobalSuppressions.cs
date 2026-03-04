// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Microsoft.Teams.Bot.Apps.UnitTests")]

[assembly: SuppressMessage("Performance",
    "CA1873:Avoid potentially expensive logging",
    Justification = "<Pending>",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Teams.Bot.Apps")]

[assembly: SuppressMessage("Performance",
    "CA1848:Use the LoggerMessage delegates",
    Justification = "<Pending>",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Teams.Bot.Apps")]

[assembly: SuppressMessage("Usage",
    "CA2227:Collection properties should be read only",
    Justification = "<Pending>",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Teams.Bot.Apps")]
