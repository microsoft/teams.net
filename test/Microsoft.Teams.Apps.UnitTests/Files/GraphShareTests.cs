// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Microsoft.Teams.Apps.Files;

namespace Microsoft.Teams.Apps.UnitTests.Files;

public class GraphShareTests
{
    /// <summary>Decode a sharing token back to the URL it was built from, so a round-trip can be asserted.</summary>
    private static string Decode(string token)
    {
        string payload = token[2..].Replace('_', '/').Replace('-', '+');

        return Encoding.UTF8.GetString(
            Convert.FromBase64String(payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=')));
    }

    [Fact]
    public void EncodeSharingUrl_ProducesTheDocumentedUForm()
    {
        // The worked example from Graph's own documentation, which is the only place the full transform (including
        // the UTF-8 step) is spelled out.
        Assert.Equal(
            "u!aHR0cHM6Ly9vbmVkcml2ZS5saXZlLmNvbS9yZWRpcj9yZXNpZD0xJmF1dGhrZXk9IXg",
            GraphShare.EncodeSharingUrl("https://onedrive.live.com/redir?resid=1&authkey=!x"));
    }

    [Fact]
    public void EncodeSharingUrl_StripsBase64Padding()
        => Assert.DoesNotContain('=', GraphShare.EncodeSharingUrl("https://a.example/b"));

    [Fact]
    public void EncodeSharingUrl_SubstitutesBothBase64UrlCharacters()
    {
        // `?` and `~` were chosen because this input's base64 contains both `+` and `/`, which is what makes the
        // substitution observable at all.
        string encoded = GraphShare.EncodeSharingUrl("https://example.com/~a?b=ÿÿ>?");

        Assert.DoesNotContain('+', encoded);
        Assert.DoesNotContain('/', encoded);
    }

    [Fact]
    public void EncodeSharingUrl_RoundTripsNonAscii_WhichIsTheCaseTheUtf8StepExistsFor()
    {
        // OneDrive paths embed the file name, so non-ASCII is routine rather than exotic. Encoding as Latin-1 would
        // silently corrupt these.
        const string url = "https://contoso.sharepoint.com/personal/a/Documents/rapport-café-café.pdf";

        Assert.Equal(url, Decode(GraphShare.EncodeSharingUrl(url)));
    }

    [Fact]
    public void EncodeSharingUrl_RoundTripsAUrlContainingSpaces()
    {
        const string url = "https://contoso.sharepoint.com/personal/a/Documents/quarterly report.docx";

        Assert.Equal(url, Decode(GraphShare.EncodeSharingUrl(url)));
    }

    [Fact]
    public void BuildDriveItemContentUrl_AppendsTheApiVersion()
    {
        // The configured value is a host root and the version is appended here, matching how a Graph client composes
        // its base URL. A test that supplied a pre-versioned base URL would assert a convention this SDK does not
        // use, and would pass while the real value produced a 404.
        Uri sharing = new("https://a.example/b");

        Assert.Equal(
            new Uri($"https://graph.microsoft.com/v1.0/shares/{GraphShare.EncodeSharingUrl("https://a.example/b")}/driveItem/content"),
            GraphShare.BuildDriveItemContentUrl(sharing, new Uri("https://graph.microsoft.com")));
    }

    [Fact]
    public void BuildDriveItemContentUrl_RoutesToTheSovereignHostWhenTheCloudSuppliesOne()
    {
        // GCCH configures `BotFramework:GraphBaseUrl` as `https://graph.microsoft.us`, a different host.
        Uri built = GraphShare.BuildDriveItemContentUrl(new Uri("https://a.example/b"), new Uri("https://graph.microsoft.us"));

        Assert.StartsWith("https://graph.microsoft.us/v1.0/shares/", built.OriginalString, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDriveItemContentUrl_DefaultsToThePublicCloudWhenNoRootIsSupplied()
        => Assert.StartsWith(
            "https://graph.microsoft.com/v1.0/shares/",
            GraphShare.BuildDriveItemContentUrl(new Uri("https://a.example/b")).OriginalString,
            StringComparison.Ordinal);

    [Fact]
    public void BuildDriveItemContentUrl_DoesNotDoubleTheSeparatorWhenTheRootHasATrailingSlash()
        => Assert.DoesNotContain(
            "//v1.0",
            GraphShare.BuildDriveItemContentUrl(new Uri("https://a.example/b"), new Uri("https://graph.microsoft.com/")).OriginalString,
            StringComparison.Ordinal);

    [Fact]
    public void BuildDriveItemContentUrl_EncodesTheUrlAsItArrivedOnTheWire()
    {
        // A .NET-only trap. `Uri.ToString()` unescapes percent-encoding, so encoding from it would produce a token
        // for a URL the service never issued, and the lookup would miss.
        Uri sharing = new("https://contoso.sharepoint.com/personal/a/quarterly%20report.docx");

        Assert.Contains(
            GraphShare.EncodeSharingUrl("https://contoso.sharepoint.com/personal/a/quarterly%20report.docx"),
            GraphShare.BuildDriveItemContentUrl(sharing).OriginalString,
            StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDriveItemContentUrl_RefusesAnHttpRoot_BecauseTheRequestCarriesABearer()
    {
        // The download URL is already required to be https and carries no bearer. This one does, so it gets at least the same check: a mistyped scheme would otherwise put a Graph token on the wire in cleartext.
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => GraphShare.BuildDriveItemContentUrl(new Uri("https://a.example/b"), new Uri("http://graph.microsoft.com")));

        Assert.Contains("must use https", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDriveItemContentUrl_AllowsHttpOnLoopback_SoALocalMockGraphStillWorks()
    {
        Uri built = GraphShare.BuildDriveItemContentUrl(new Uri("https://a.example/b"), new Uri("http://localhost:3000"));

        Assert.StartsWith("http://localhost:3000/v1.0/shares/", built.OriginalString, StringComparison.Ordinal);
    }
}
