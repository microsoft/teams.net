﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Cards;

/// <summary>
/// A thumbnail card (card with a single, small thumbnail image)
/// </summary>
public class ThumbnailCard : Card
{
    /// <summary>
    /// Array of images for the card
    /// </summary>
    [JsonPropertyName("images")]
    [JsonPropertyOrder(4)]
    public IList<Image>? Images { get; set; }

    /// <summary>
    /// This action will be activated when user taps on the card itself
    /// </summary>
    [JsonPropertyName("tap")]
    [JsonPropertyOrder(5)]
    public Action? Tap { get; set; }
}