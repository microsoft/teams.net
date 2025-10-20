// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace Microsoft.Teams.Api.Entities;

public class ProductInfoEntity : Entity
{
    [JsonPropertyName("id")]
    [JsonPropertyOrder(3)]
    public string? Id { get; set; }

    public ProductInfoEntity() : base("ProductInfo") { }
}
