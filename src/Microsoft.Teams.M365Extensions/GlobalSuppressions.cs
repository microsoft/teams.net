// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance",
    "CA1848:Use the LoggerMessage delegates",
    Justification = "Diagnostic logging on the routing path; readability preferred over source-generated delegates.",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Teams.M365Extensions")]

[assembly: SuppressMessage("Performance",
    "CA1873:Avoid potentially expensive logging",
    Justification = "Debug-level routing diagnostics use only already-materialized activity ids.",
    Scope = "namespaceanddescendants",
    Target = "~N:Microsoft.Teams.M365Extensions")]

[assembly: SuppressMessage("Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "Instantiated by the dependency injection container as a DelegatingHandler.",
    Scope = "type",
    Target = "~T:Microsoft.Teams.M365Extensions.AgentSdkAuthHandler")]
