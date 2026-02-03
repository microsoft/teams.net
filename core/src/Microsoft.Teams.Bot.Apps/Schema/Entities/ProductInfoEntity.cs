// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Bot.Apps.Schema.Entities;




/// <summary>
/// Product info entity.
/// </summary>
public class ProductInfoEntity : Entity
{
    /// <summary>
    /// Creates a new instance of <see cref="ProductInfoEntity"/>.
    /// </summary>
    public ProductInfoEntity() : base("ProductInfo") { }

    /// <summary>
    /// Gets or sets the product id.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id
    {
        get => base.Properties.TryGetValue("id", out var value) ? value?.ToString() : null;
        set => base.Properties["id"] = value;
    }
}
