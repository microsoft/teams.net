// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Teams.Apps.Schema.Entities;

namespace Microsoft.Teams.Apps.UnitTests;

/// <summary>
/// Verifies that the CitationEntity copy constructor deep-copies CitationClaim instances
/// so that mutations on the copy do not affect the original (COPY-01 / A-015).
/// </summary>
public class CitationEntityDeepCopyTests
{
    private static CitationEntity MakeCitation() => new()
    {
        OType = "Message",
        OContext = "https://schema.org",
        Type = "message",
        Citation =
        [
            new CitationClaim
            {
                Position = 1,
                Appearance = new CitationAppearanceDocument
                {
                    Name = "Source A",
                    Abstract = "Extract from Source A",
                    Url = new Uri("https://example.com/a"),
                    EncodingFormat = "text/plain"
                }
            }
        ]
    };

    [Fact]
    public void CopyConstructor_Citation_IsDeepCopied()
    {
        // Arrange
        CitationEntity original = MakeCitation();

        // Act – create a copy via the OMessageEntity copy constructor
        CitationEntity copy = new(original);

        // Mutate the copy's first claim
        copy.Citation![0].Appearance.Name = "Mutated Name";

        // Assert – original must be unaffected
        Assert.Equal("Source A", original.Citation![0].Appearance.Name);
    }

    [Fact]
    public void CopyConstructor_Citation_ListIsIndependent()
    {
        // Arrange
        CitationEntity original = MakeCitation();
        CitationEntity copy = new(original);

        // Act – add an item to the copy's list
        copy.Citation!.Add(new CitationClaim
        {
            Position = 99,
            Appearance = new CitationAppearanceDocument { Name = "Extra", Abstract = "Extra abstract" }
        });

        // Assert – original list must not grow
        Assert.Single(original.Citation!);
    }
}
