// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

using Json.Schema;
using Json.Schema.Generation;

namespace Microsoft.Teams.AI.Tests;

/// <summary>
/// Equivalence tests comparing JsonSchema.Net with NJsonSchema (via JsonSchemaWrapper).
/// These tests ensure the migration to NJsonSchema maintains behavioral compatibility.
/// This file can be removed once the migration is validated and merged.
/// </summary>
public class JsonSchemaEquivalenceTests
{
    #region Schema Generation Equivalence

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(double))]
    [InlineData(typeof(long))]
    public void GenerateSchema_PrimitiveTypes_BothLibrariesValidateSameWay(Type type)
    {
        // Arrange - Generate schemas using both libraries
        var jsonSchemaNet = new JsonSchemaBuilder().FromType(type).Build();
        var nJsonSchema = JsonSchemaWrapper.FromType(type);

        // Get valid test values for each type
        var validJson = GetValidJsonForType(type);
        var invalidJson = GetInvalidJsonForType(type);

        // Act & Assert - Both should accept valid input
        var jsonSchemaNetValid = jsonSchemaNet.Evaluate(JsonNode.Parse(validJson)).IsValid;
        var nJsonSchemaValid = nJsonSchema.Validate(validJson).IsValid;
        Assert.Equal(jsonSchemaNetValid, nJsonSchemaValid);

        // Act & Assert - Both should reject invalid input
        var jsonSchemaNetInvalid = jsonSchemaNet.Evaluate(JsonNode.Parse(invalidJson)).IsValid;
        var nJsonSchemaInvalid = nJsonSchema.Validate(invalidJson).IsValid;
        Assert.Equal(jsonSchemaNetInvalid, nJsonSchemaInvalid);
    }

    [Fact]
    public void GenerateSchema_ComplexType_BothLibrariesValidateSameWay()
    {
        // Arrange
        var jsonSchemaNet = new JsonSchemaBuilder().FromType(typeof(TestPerson)).Build();
        var nJsonSchema = JsonSchemaWrapper.FromType(typeof(TestPerson));

        var validJson = """{"Name":"John","Age":30}""";
        var invalidJson = """{"Name":123,"Age":"not a number"}""";

        // Act & Assert - Valid
        var jsonSchemaNetValid = jsonSchemaNet.Evaluate(JsonNode.Parse(validJson)).IsValid;
        var nJsonSchemaValid = nJsonSchema.Validate(validJson).IsValid;
        Assert.Equal(jsonSchemaNetValid, nJsonSchemaValid);

        // Act & Assert - Invalid
        var jsonSchemaNetInvalid = jsonSchemaNet.Evaluate(JsonNode.Parse(invalidJson)).IsValid;
        var nJsonSchemaInvalid = nJsonSchema.Validate(invalidJson).IsValid;
        Assert.Equal(jsonSchemaNetInvalid, nJsonSchemaInvalid);
    }

    #endregion

    #region Schema Parsing Equivalence

    [Theory]
    [InlineData("""{"type":"string"}""")]
    [InlineData("""{"type":"integer"}""")]
    [InlineData("""{"type":"boolean"}""")]
    [InlineData("""{"type":"object","properties":{"name":{"type":"string"}}}""")]
    [InlineData("""{"type":"object","properties":{"value":{"type":"number"}},"required":["value"]}""")]
    public void ParseSchema_ValidJsonSchema_BothLibrariesParse(string schemaJson)
    {
        // Act - Both should parse without error
        var jsonSchemaNet = JsonSchema.FromText(schemaJson);
        var nJsonSchema = JsonSchemaWrapper.FromJson(schemaJson);

        // Assert - Both parsed successfully
        Assert.NotNull(jsonSchemaNet);
        Assert.NotNull(nJsonSchema);
    }

    #endregion

    #region Validation Equivalence

    [Fact]
    public void Validate_RequiredProperty_BothLibrariesRejectMissing()
    {
        // Arrange
        var schemaJson = """{"type":"object","properties":{"name":{"type":"string"}},"required":["name"]}""";
        var jsonSchemaNet = JsonSchema.FromText(schemaJson);
        var nJsonSchema = JsonSchemaWrapper.FromJson(schemaJson);

        var validJson = """{"name":"test"}""";
        var missingRequired = """{}""";

        // Act & Assert - Valid input
        Assert.True(jsonSchemaNet.Evaluate(JsonNode.Parse(validJson)).IsValid);
        Assert.True(nJsonSchema.Validate(validJson).IsValid);

        // Act & Assert - Missing required
        Assert.False(jsonSchemaNet.Evaluate(JsonNode.Parse(missingRequired)).IsValid);
        Assert.False(nJsonSchema.Validate(missingRequired).IsValid);
    }

    [Fact]
    public void Validate_TypeMismatch_BothLibrariesReject()
    {
        // Arrange
        var schemaJson = """{"type":"object","properties":{"count":{"type":"integer"}}}""";
        var jsonSchemaNet = JsonSchema.FromText(schemaJson);
        var nJsonSchema = JsonSchemaWrapper.FromJson(schemaJson);

        var validJson = """{"count":42}""";
        var wrongType = """{"count":"not a number"}""";

        // Act & Assert - Valid
        Assert.True(jsonSchemaNet.Evaluate(JsonNode.Parse(validJson)).IsValid);
        Assert.True(nJsonSchema.Validate(validJson).IsValid);

        // Act & Assert - Wrong type
        Assert.False(jsonSchemaNet.Evaluate(JsonNode.Parse(wrongType)).IsValid);
        Assert.False(nJsonSchema.Validate(wrongType).IsValid);
    }

    #endregion

    #region Function Parameter Schema Equivalence

    [Fact]
    public void FunctionParameters_SingleStringParam_BothLibrariesValidateSameWay()
    {
        // Arrange - Build object schema with string property (mimics Function.GenerateParametersSchema)
        var jsonSchemaNet = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(("text", new JsonSchemaBuilder().Type(SchemaValueType.String).Build()))
            .Required("text")
            .Build();

        var nJsonSchema = JsonSchemaWrapper.CreateObjectSchema(
            ("text", JsonSchemaWrapper.String(), true)
        );

        var validJson = """{"text":"hello"}""";
        var missingText = """{}""";
        var wrongType = """{"text":123}""";

        // Act & Assert - Valid
        Assert.True(jsonSchemaNet.Evaluate(JsonNode.Parse(validJson)).IsValid);
        Assert.True(nJsonSchema.Validate(validJson).IsValid);

        // Act & Assert - Missing required
        Assert.False(jsonSchemaNet.Evaluate(JsonNode.Parse(missingText)).IsValid);
        Assert.False(nJsonSchema.Validate(missingText).IsValid);

        // Act & Assert - Wrong type
        Assert.False(jsonSchemaNet.Evaluate(JsonNode.Parse(wrongType)).IsValid);
        Assert.False(nJsonSchema.Validate(wrongType).IsValid);
    }

    [Fact]
    public void FunctionParameters_MultipleParams_BothLibrariesValidateSameWay()
    {
        // Arrange
        var jsonSchemaNet = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(
                ("name", new JsonSchemaBuilder().Type(SchemaValueType.String).Build()),
                ("count", new JsonSchemaBuilder().Type(SchemaValueType.Integer).Build())
            )
            .Required("name", "count")
            .Build();

        var nJsonSchema = JsonSchemaWrapper.CreateObjectSchema(
            ("name", JsonSchemaWrapper.FromType(typeof(string)), true),
            ("count", JsonSchemaWrapper.FromType(typeof(int)), true)
        );

        var validJson = """{"name":"test","count":5}""";
        var missingCount = """{"name":"test"}""";

        // Act & Assert - Valid
        Assert.True(jsonSchemaNet.Evaluate(JsonNode.Parse(validJson)).IsValid);
        Assert.True(nJsonSchema.Validate(validJson).IsValid);

        // Act & Assert - Missing required
        Assert.False(jsonSchemaNet.Evaluate(JsonNode.Parse(missingCount)).IsValid);
        Assert.False(nJsonSchema.Validate(missingCount).IsValid);
    }

    #endregion

    #region Test Helpers

    private static string GetValidJsonForType(Type type)
    {
        return type.Name switch
        {
            "String" => "\"hello\"",
            "Int32" => "42",
            "Boolean" => "true",
            "Double" => "3.14",
            "Int64" => "9999999999",
            _ => "null"
        };
    }

    private static string GetInvalidJsonForType(Type type)
    {
        // Return a value that doesn't match the expected type
        return type.Name switch
        {
            "String" => "123",             // number instead of string
            "Int32" => "\"not a number\"", // string instead of int
            "Boolean" => "\"yes\"",        // string instead of bool
            "Double" => "\"not a double\"", // string instead of double
            "Int64" => "\"not a long\"",   // string instead of long
            _ => "[]"
        };
    }

    private class TestPerson
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    #endregion
}
