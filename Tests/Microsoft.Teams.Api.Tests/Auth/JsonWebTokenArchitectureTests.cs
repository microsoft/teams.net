// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace Microsoft.Teams.Api.Tests.Auth;

/// <summary>
/// Architectural test: locks the trust-boundary contract on JsonWebToken.
///
/// JsonWebToken is a typed accessor over an *already-validated* JWT payload.
/// Constructing it does not verify the signature. Legitimate construction sites
/// must either run after the JwtBearer middleware has executed (i.e. inside an
/// endpoint chained with .RequireAuthorization(...)) or wrap a token sourced
/// from trusted identity infrastructure (MSAL, Bot Framework API responses).
///
/// If a new file starts constructing JsonWebToken, this test fails so the
/// addition is reviewed against the trust-boundary contract. Add the file to
/// ALLOWLIST below only after verifying it satisfies one of those conditions.
/// </summary>
public class JsonWebTokenArchitectureTests
{
    private static readonly HashSet<string> Allowlist = new()
    {
        // Token responses from authenticated Microsoft identity exchanges —
        // tokens are sourced from trusted infrastructure.
        "Libraries/Microsoft.Teams.Apps/App.cs",
        // Assigns UserGraphToken from an authenticated Teams API exchange.
        "Libraries/Microsoft.Teams.Apps/AppRouting.cs",
        // Extracts the inbound JWT inside a handler that requires
        // RequireAuthorization upstream — JwtBearer has validated the signature
        // before this code runs.
        "Libraries/Microsoft.Teams.Plugins/Microsoft.Teams.Plugins.AspNetCore/AspNetCorePlugin.cs",
        // Devtools controller — environment-guarded against production by the
        // DevToolsPlugin's hosting-environment check.
        "Libraries/Microsoft.Teams.Plugins/Microsoft.Teams.Plugins.AspNetCore.DevTools/Controllers/ActivityController.cs",
    };

    private static readonly Regex ConstructPattern = new(@"\bnew\s+JsonWebToken\s*\(", RegexOptions.Compiled);

    private const string DefinitionFile = "Libraries/Microsoft.Teams.Api/Auth/JsonWebToken.cs";
    private static readonly string[] SkipDirs = ["bin", "obj", ".git", "node_modules"];

    [Fact]
    public void JsonWebToken_IsConstructedOnlyAtAllowlistedSites()
    {
        var repoRoot = FindRepoRoot();
        var librariesRoot = Path.Combine(repoRoot, "Libraries");
        Assert.True(Directory.Exists(librariesRoot), $"Libraries directory not found under {repoRoot}");

        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(librariesRoot, "*.cs", SearchOption.AllDirectories))
        {
            var segments = Path.GetRelativePath(repoRoot, file).Split(Path.DirectorySeparatorChar);
            if (segments.Any(seg => SkipDirs.Contains(seg))) continue;

            var relative = Path.GetRelativePath(repoRoot, file).Replace(Path.DirectorySeparatorChar, '/');
            if (relative == DefinitionFile) continue;

            var contents = File.ReadAllText(file);
            if (!ConstructPattern.IsMatch(contents)) continue;
            if (!Allowlist.Contains(relative))
            {
                offenders.Add(relative);
            }
        }

        Assert.True(
            offenders.Count == 0,
            "JsonWebToken construction found outside the allowlisted trust-boundary sites:\n" +
            string.Join("\n", offenders) +
            "\nIf this is intentional, verify the new site runs after a TokenValidator pass " +
            "or wraps a token from trusted identity infrastructure, then add it to Allowlist " +
            "in this test."
        );
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Microsoft.Teams.sln")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate repo root (Microsoft.Teams.sln)");
    }
}
