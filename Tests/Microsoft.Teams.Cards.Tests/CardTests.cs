
using System.Text.Json;

namespace Microsoft.Teams.Cards.Tests;
public class CardTests
{
    [Fact]
    public void validateCardDefault()
    {
        
        var card = new Card();

        var json = JsonSerializer.Serialize(card, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        var exectedObject = new
        {
            type = "AdaptiveCard",
            version="1.6"
        };
        var exectedObjectJson = JsonSerializer.Serialize(exectedObject, new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(exectedObjectJson, json);
    }
}
