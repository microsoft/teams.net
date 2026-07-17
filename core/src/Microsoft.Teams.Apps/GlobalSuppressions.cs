// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance",
    "CA1873:Avoid potentially expensive logging",
    Justification = "<Pending>",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Teams.Apps")]

[assembly: SuppressMessage("Performance",
    "CA1848:Use the LoggerMessage delegates",
    Justification = "<Pending>",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Teams.Apps")]

[assembly: SuppressMessage("Usage",
    "CA2227:Collection properties should be read only",
    Justification = "<Pending>",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Teams.Apps")]

[assembly: SuppressMessage("Naming",
    "CA1724:Type names should not match namespaces",
    Justification = "Microsoft.Teams.Apps.Context<T> is part of the public API and predates the OpenTelemetry.Api dep. The OpenTelemetry.Context namespace clash is benign because consumers always disambiguate via using directives.",
    Scope = "type",
    Target = "~T:Microsoft.Teams.Apps.Context`1")]
