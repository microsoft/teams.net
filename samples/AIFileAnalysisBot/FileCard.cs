// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using Microsoft.Teams.Apps.Files;
using Microsoft.Teams.Cards;

namespace AIFileAnalysisBot;

internal static class FileCard
{
    /// <summary>
    /// FILE RECEIVE: the no-LLM response for a file this sample will not send to the model.
    ///
    /// Nothing here touches Azure OpenAI. It reports what the file API exposes (scope, source, resolved content type)
    /// plus the byte count that was actually downloaded, so the file round-trip is still demonstrated for formats the
    /// model never sees.
    /// </summary>
    public static JsonElement Unsupported(IncomingFile file, DownloadedFile downloaded)
    {
        AdaptiveCard card = new([
            new Container(
                new TextBlock("File received")
                {
                    Weight = TextWeight.Bolder,
                    Size = TextSize.Large,
                    Color = TextColor.Accent,
                },
                new TextBlock(downloaded.Filename)
                {
                    Weight = TextWeight.Bolder,
                    Wrap = true,
                })
            {
                Style = ContainerStyle.Emphasis,
            },
            new FactSet(
                new Fact("Type", downloaded.ContentType),
                new Fact("Size", HumanSize(downloaded.Bytes.Length)),
                new Fact("Scope", file.Scope.ToString()),
                new Fact("Source", file.Source.ToString())),
            new TextBlock(
                "I downloaded this file but did not analyze it. This sample sends only text files and PNG, JPEG, GIF, "
                + "or WebP images to the model.")
            {
                Wrap = true,
                IsSubtle = true,
                Spacing = Spacing.Medium,
            }])
        {
            Version = Microsoft.Teams.Cards.Version.Version1_5,
        };

        return JsonSerializer.SerializeToElement(card);
    }

    private static string HumanSize(int bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        string[] units = ["KB", "MB", "GB"];
        double value = bytes / 1024.0;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:F1} {units[unit]}";
    }
}
