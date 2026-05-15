// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Apps.Schema.Entities;




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
        get => base.Properties.TryGetValue("id", out object? value) ? value?.ToString() : null;
        set => base.Properties["id"] = value;
    }
}

/// <summary>
/// Product info entity extension methods.
/// </summary>
public static class ProductInfoEntityExtensions
{
    /// <summary>
    /// Gets the first product info entity from the activity.
    /// </summary>
    public static ProductInfoEntity? GetProductInfo(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        if (activity.Entities == null)
        {
            return null;
        }

        return activity.Entities.FirstOrDefault(e => e is ProductInfoEntity) as ProductInfoEntity;
    }
}
