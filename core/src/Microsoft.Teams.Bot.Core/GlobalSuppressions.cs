// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

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
