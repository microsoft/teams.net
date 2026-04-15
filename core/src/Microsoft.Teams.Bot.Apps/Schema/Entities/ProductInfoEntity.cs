// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Teams.Bot.Apps.Schema;

namespace Microsoft.Teams.Bot.Apps.Schema.Entities;

/// <summary>
/// Extension methods for activity product info.
/// </summary>
public static class ActivityProductInfoExtensions
{
    /// <summary>
    /// Adds a product info entity to the activity.
    /// </summary>
    /// <param name="activity">The activity to add product info to. Cannot be null.</param>
    /// <param name="id">The product identifier.</param>
    /// <returns>The created ProductInfoEntity that was added to the activity.</returns>
    public static ProductInfoEntity AddProductInfo(this TeamsActivity activity, string id)
    {
        ArgumentNullException.ThrowIfNull(activity);
        ProductInfoEntity productInfo = new() { Id = id };
        activity.Entities ??= [];
        activity.Entities.Add(productInfo);
        activity.Rebase();
        return productInfo;
    }

    /// <summary>
    /// Gets the product info entity from the activity's entity collection, if present.
    /// </summary>
    /// <param name="activity">The activity to read from. Cannot be null.</param>
    /// <returns>The ProductInfoEntity if found; otherwise, null.</returns>
    public static ProductInfoEntity? GetProductInfo(this TeamsActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return activity.Entities?.FirstOrDefault(e => e is ProductInfoEntity) as ProductInfoEntity;
    }
}




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
