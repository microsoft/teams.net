using System.Text.Json;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.AI.Prompts;
using Microsoft.Teams.AI.Templates;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;

namespace Samples.AI.Handlers;

public static class FunctionCallingHandler
{
    /// <summary>
    /// Handle Pokemon search using PokeAPI
    /// </summary>
    public static async Task<string> PokemonSearchFunction([Param("pokemon_name")] string pokemonName)
    {
        Console.WriteLine($"[FUNCTION] pokemon_search called with pokemon_name='{pokemonName}'");

        try
        {
            using var client = new HttpClient();
            Console.WriteLine($"[FUNCTION] Fetching Pokemon data from PokeAPI for '{pokemonName}'...");

            var response = await client.GetAsync($"https://pokeapi.co/api/v2/pokemon/{pokemonName.ToLower()}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[FUNCTION] Pokemon '{pokemonName}' not found (status: {response.StatusCode})");
                return $"Pokemon '{pokemonName}' not found";
            }
            else
            {
                Console.WriteLine($"[FUNCTION] Successfully retrieved data for Pokemon '{pokemonName}'");
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonDocument.Parse(json);
            var root = data.RootElement;

            var name = root.GetProperty("name").GetString();
            var height = root.GetProperty("height").GetInt32();
            var weight = root.GetProperty("weight").GetInt32();
            var types = root.GetProperty("types")
                .EnumerateArray()
                .Select(t => t.GetProperty("type").GetProperty("name").GetString())
                .ToList();

            var result = $"Pokemon {name}: height={height}, weight={weight}, types={string.Join(", ", types)}";
            Console.WriteLine($"[FUNCTION] Successfully retrieved Pokemon data: {result}");

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FUNCTION] Error searching for Pokemon: {ex.Message}");
            return $"Error searching for Pokemon: {ex.Message}";
        }
    }

    /// <summary>
    /// Handle single function calling - Pokemon search
    /// </summary>
    public static async Task HandlePokemonSearch(OpenAIChatModel model, IContext<MessageActivity> context)
    {
        Console.WriteLine($"[HANDLER] Pokemon search handler invoked with text: '{context.Activity.Text}'");

        var prompt = new OpenAIChatPrompt(model, new ChatPromptOptions
        {
            Instructions = new StringTemplate("You are a helpful assistant that can look up Pokemon for the user.")
        });

        // Register the pokemon search function
        prompt.Function(
            "pokemon_search",
            "Search for pokemon information including height, weight, and types",
            PokemonSearchFunction
        );

        Console.WriteLine("[HANDLER] Registered pokemon_search function, sending prompt to AI...");
        var result = await prompt.Send(context.Activity.Text);

        if (result.Content != null)
        {
            Console.WriteLine($"[HANDLER] AI response received: {result.Content}");
            var message = new MessageActivity
            {
                Text = result.Content,
            }.AddAIGenerated();
            await context.Send(message);
        }
        else
        {
            Console.WriteLine("[HANDLER] No content received from AI");
            await context.Reply("Sorry I could not find that pokemon");
        }
    }

}
