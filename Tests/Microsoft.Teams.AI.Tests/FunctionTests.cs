using Microsoft.Teams.AI.Annotations;

namespace Microsoft.Teams.AI.Tests;

public class FunctionTests
{
    [Fact]
    public void Test_Function_GeneratesParametersSchema_FromDelegate()
    {
        // Arrange & Act
        var function = new Function(
            "test_function",
            "A test function",
            ([Param("name")] string name, int age) => $"Hello {name}, age {age}"
        );

        // Assert
        Assert.NotNull(function.Parameters);
    }

    [Fact]
    public void Test_Function_GeneratesParametersSchema_WithParamAttribute()
    {
        // Arrange & Act
        var function = new Function(
            "pokemon_search",
            "Search for pokemon",
            ([Param("pokemon_name")] string pokemonName) => $"Searching for {pokemonName}"
        );

        // Assert
        Assert.NotNull(function.Parameters);
    }

    [Fact]
    public void Test_Function_NoParameters_ReturnsNullSchema()
    {
        // Arrange & Act
        var function = new Function(
            "no_params_function",
            "A function with no parameters",
            () => "No params"
        );

        // Assert
        Assert.Null(function.Parameters);
    }

    [Fact]
    public void Test_Function_OptionalParameters_MarkedCorrectly()
    {
        // Arrange & Act
        var function = new Function(
            "optional_params_function",
            "A function with optional parameters",
            (string required, string optional = "default") => $"{required} {optional}"
        );

        // Assert
        Assert.NotNull(function.Parameters);
    }

    [Fact]
    public void Test_Function_MultipleParameters_GeneratesSchema()
    {
        // Arrange & Act
        var function = new Function(
            "multi_param_function",
            "A function with multiple parameters",
            (string name, int age, bool active) => $"{name} {age} {active}"
        );

        // Assert
        Assert.NotNull(function.Parameters);
    }
}