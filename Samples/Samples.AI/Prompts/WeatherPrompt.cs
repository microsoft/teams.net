using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;

namespace Samples.AI.Prompts;

[Prompt]
[Prompt.Description("Weather assistant")]
[Prompt.Instructions("You are a helpful assistant that can help the user get the weather. First get their location, then get the weather for that location.")]
public class WeatherPrompt(IContext<IActivity> context)
{
    [Function]
    [Function.Description("Gets the location of the user")]
    public string GetUserLocation()
    {
        var locations = new[] { "Seattle", "San Francisco", "New York" };
        var random = new Random();
        var location = locations[random.Next(locations.Length)];

        context.Log.Info($"[PROMPT-FUNCTION] get_user_location called, returning mock location: '{location}'");
        return location;
    }

    [Function]
    [Function.Description("Search for weather at a specific location")]
    public string WeatherSearch([Param] string location)
    {
        context.Log.Info($"[PROMPT-FUNCTION] weather_search called with location='{location}'");

        var weatherByLocation = new Dictionary<string, (int Temperature, string Condition)>
        {
            ["Seattle"] = (65, "sunny"),
            ["San Francisco"] = (60, "foggy"),
            ["New York"] = (75, "rainy")
        };

        if (!weatherByLocation.TryGetValue(location, out var weather))
        {
            context.Log.Info($"[PROMPT-FUNCTION] Weather data not found for location '{location}'");
            return "Sorry, I could not find the weather for that location";
        }

        var result = $"The weather in {location} is {weather.Condition} with a temperature of {weather.Temperature}°F";
        context.Log.Info($"[PROMPT-FUNCTION] Returning weather data: {result}");
        return result;
    }
}