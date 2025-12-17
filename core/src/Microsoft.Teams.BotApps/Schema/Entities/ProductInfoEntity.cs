// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace Microsoft.Teams.BotApps.Schema.Entities;




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
    /// Ids the product id.
    /// </summary>
    [JsonPropertyName("id")] public string? Id { get; set; }

    /// <summary>
    /// Adds custom fields as properties.
    /// </summary>
    public override void ToProperties()
    {

    }
}
