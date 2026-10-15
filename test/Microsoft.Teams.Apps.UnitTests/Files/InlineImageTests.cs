// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Teams.Apps.Files;

namespace Microsoft.Teams.Apps.UnitTests.Files;

public class InlineImageTests
{
    private const string ItemType = "http://schema.skype.com/AMSImage";

    private static Dictionary<string, string> Hints(string html) => InlineImage.ParseInlineImageHints(html);

    private static string? Id(string url) => InlineImage.InlineImageId(new Uri(url));

    [Fact]
    public void SniffImageFormat_RecognizesGif87aAsWellAsGif89a()
        => Assert.Equal(InlineImageFormat.Gif, InlineImage.SniffImageFormat([0x47, 0x49, 0x46, 0x38, 0x37, 0x61]));

    [Fact]
    public void SniffImageFormat_DoesNotTreatARiffContainerOtherThanWebPAsWebP()
    {
        byte[] wav = [0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x41, 0x56, 0x45];

        Assert.Null(InlineImage.SniffImageFormat(wav));
    }

    [Fact]
    public void SniffImageFormat_DoesNotMatchASignatureLongerThanTheBytes()
    {
        Assert.Null(InlineImage.SniffImageFormat([0x89, 0x50, 0x4e]));
        Assert.Null(InlineImage.SniffImageFormat([0x52, 0x49, 0x46, 0x46]));
        Assert.Null(InlineImage.SniffImageFormat([]));
    }

    [Fact]
    public void FormatFromHint_AcceptsJpgAndAnyCase_AndRejectsNamesThatAreOnlyObjectKeys()
    {
        Assert.Equal(InlineImageFormat.Jpeg, InlineImage.FormatFromHint("JPG"));
        Assert.Equal(InlineImageFormat.Png, InlineImage.FormatFromHint(" Png "));
        Assert.Null(InlineImage.FormatFromHint("constructor"));
        Assert.Null(InlineImage.FormatFromHint("toString"));
    }

    [Fact]
    public void InlineImageId_ReadsTheIdOnlyFromAnInlineImageLocation()
    {
        Assert.Equal("0-wus-d1-abc", Id("https://smba.trafficmanager.net/amer/v3/attachments/0-wus-d1-abc/views/original"));
        Assert.Null(Id("https://smba.trafficmanager.net/amer/v3/attachments/0-wus-d1-abc/views/thumbnail"));
    }

    [Fact]
    public void InlineImageId_RejectsAnIdThatCouldCarryAPathSeparatorIntoTheFileName()
    {
        Assert.Null(Id("https://smba.trafficmanager.net/amer/v3/attachments/a\\..\\..\\evil/views/original"));
        Assert.Null(Id("https://smba.trafficmanager.net/amer/v3/attachments/a%5C..%5Cevil/views/original"));
    }

    [Fact]
    public void InlineImageId_RejectsAnIdWithNonAsciiLetters()
    {
        // The parsed path holds a non-ASCII letter percent-encoded, and `%` is outside the id's character set.
        Assert.Null(Id("https://smba.trafficmanager.net/amer/v3/attachments/caf\u00e9/views/original"));
        Assert.Null(Id("https://smba.trafficmanager.net/amer/v3/attachments/\u0430\u0431\u0432/views/original"));
    }

    [Fact]
    public void InlineImageId_ReadsTheIdOnlyFromThePath_NotFromAQueryStringOrFragment()
    {
        Assert.Null(Id("https://smba.trafficmanager.net/amer/v3/conversations/x/members?x=/v3/attachments/a/views/original"));
        Assert.Null(Id("https://smba.trafficmanager.net/amer/v3/conversations/x/members#/v3/attachments/a/views/original"));
        Assert.Equal("a", Id("https://smba.trafficmanager.net/amer/v3/attachments/a/views/original?x=1"));
    }

    [Fact]
    public void InlineImageId_ReturnsNull_ForARelativeOrMissingUrl()
    {
        Assert.Null(InlineImage.InlineImageId(new Uri("/v3/attachments/a/views/original", UriKind.Relative)));
        Assert.Null(InlineImage.InlineImageId(null));
    }

    [Fact]
    public void ParseInlineImageHints_ReadsDoubleQuotedSingleQuotedAndMixedAttributeValues()
    {
        Assert.Equal(new Dictionary<string, string> { ["a"] = "png" }, Hints($"<img itemtype=\"{ItemType}\" itemscope=\"png\" itemid=\"a\">"));
        Assert.Equal(new Dictionary<string, string> { ["a"] = "png" }, Hints($"<img itemtype='{ItemType}' itemscope='png' itemid='a'>"));
        Assert.Equal(new Dictionary<string, string> { ["a"] = "png" }, Hints($"<img itemtype=\"{ItemType}\" itemscope='png' itemid=\"a\">"));
    }

    [Fact]
    public void ParseInlineImageHints_DoesNotReadAnAttributeFromInsideTheValueOfAnotherAttribute()
    {
        Assert.Equal(new Dictionary<string, string> { ["a"] = "png" }, Hints($"<img itemtype=\"{ItemType}\" itemscope=\"png\" alt=\"a itemscope='gif'\" itemid=\"a\">"));
        Assert.Equal(new Dictionary<string, string> { ["a"] = "png" }, Hints($"<img itemtype=\"{ItemType}\" itemscope=\"png\" alt='a itemscope=\"gif\"' itemid=\"a\">"));
    }

    [Fact]
    public void ParseInlineImageHints_ReadsATagHoldingALongRunOfNameCharactersInLinearTime()
    {
        Stopwatch started = Stopwatch.StartNew();

        Assert.Equal(new Dictionary<string, string> { ["a"] = "png" }, Hints($"<img itemtype=\"{ItemType}\" itemscope=\"png\" itemid=\"a\" {new string('x', 50_000)}>"));
        Assert.True(started.ElapsedMilliseconds < 1000, $"took {started.ElapsedMilliseconds}ms");
    }

    [Theory]
    [InlineData("<img ")]
    [InlineData("<img \"")]
    [InlineData("<img '")]
    [InlineData("<img a=\"x")]
    [InlineData("<img a=\"x\" ")]
    [InlineData("<img a=\"' ")]
    public void ParseInlineImageHints_ReadsManyUnclosedTagsInLinearTime(string fragment)
    {
        string html = string.Concat(Enumerable.Repeat(fragment, 100_000));
        Stopwatch started = Stopwatch.StartNew();

        Assert.Empty(Hints(html));
        Assert.True(started.ElapsedMilliseconds < 2000, $"took {started.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void ParseInlineImageHints_ReadsATagWhoseQuotedValueHoldsAGreaterThanSign()
        => Assert.Equal(new Dictionary<string, string> { ["a"] = "png" }, Hints($"<img alt=\"1 > 0\" src=\"x\" itemtype=\"{ItemType}\" itemscope=\"png\" itemid=\"a\">"));

    [Fact]
    public void ParseInlineImageHints_MatchesTagAndAttributeNamesInAnyCase()
        => Assert.Equal(new Dictionary<string, string> { ["a"] = "png" }, Hints($"<IMG ITEMTYPE=\"{ItemType}\" ItemScope=\"png\" ITEMID=\"a\">"));

    [Fact]
    public void ParseInlineImageHints_MapsEachInlineImageInTheMessageToItsOwnHint()
    {
        string html = $"<p><img itemtype=\"{ItemType}\" itemscope=\"png\" itemid=\"a\"></p><p><img itemtype=\"{ItemType}\" itemscope=\"jpeg\" itemid=\"b\"></p>";

        Assert.Equal(new Dictionary<string, string> { ["a"] = "png", ["b"] = "jpeg" }, Hints(html));
    }

    [Fact]
    public void ParseInlineImageHints_IgnoresAnImageThatIsNotMarkedAsAnInlineImage()
    {
        Assert.Empty(Hints("<img itemtype=\"http://example.com/Other\" itemscope=\"png\" itemid=\"a\">"));
        Assert.Empty(Hints("<img src=\"https://example.com/a.png\" itemscope=\"png\" itemid=\"a\">"));
    }

    [Fact]
    public void ParseInlineImageHints_ReadsNoHintFromAnUnquotedOrBareAttribute_SoTheImageFallsBackToAnUnknownFormat()
    {
        Assert.Empty(Hints($"<img itemtype=\"{ItemType}\" itemscope=png itemid=\"a\">"));
        Assert.Empty(Hints($"<img itemtype=\"{ItemType}\" itemscope itemid=\"a\">"));
    }

    [Fact]
    public void ParseInlineImageHints_DoesNotDecodeCharacterReferences_SoAnEncodedHintIsNotAKnownFormat()
    {
        Dictionary<string, string> encoded = Hints($"<img itemtype=\"{ItemType}\" itemscope=\"p&#110;g\" itemid=\"a\">");

        Assert.Equal(new Dictionary<string, string> { ["a"] = "p&#110;g" }, encoded);
        Assert.Null(InlineImage.FormatFromHint(encoded["a"]));
        Assert.Empty(Hints("<img itemtype=\"http:&#x2F;&#x2F;schema.skype.com/AMSImage\" itemscope=\"png\" itemid=\"a\">"));
    }
}
