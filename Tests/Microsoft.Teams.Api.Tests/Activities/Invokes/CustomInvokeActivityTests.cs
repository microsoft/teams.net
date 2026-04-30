using System.Reflection;
using System.Text.Json;

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;

namespace Microsoft.Teams.Api.Tests.Activities;

public class CustomInvokeActivityTests
{
    [Fact]
    public void CustomInvokeActivity_DeserializesAsBaseInvokeActivity()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/CustomInvokeActivity.json");

        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);

        Assert.NotNull(activity);
        Assert.Equal(typeof(InvokeActivity), activity!.GetType());
        Assert.Equal("suggestedActions/submit", activity.Name.Value);
        Assert.NotNull(activity.Value);
    }

    [Fact]
    public void CustomInvokeActivity_PopulatesEveryActivityProperty()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/CustomInvokeActivity.json");

        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);
        Assert.NotNull(activity);

        var doc = JsonDocument.Parse(json);
        var jsonProperties = doc.RootElement.EnumerateObject()
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unpopulated = new List<string>();
        foreach (var prop in typeof(InvokeActivity).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var jsonName = prop.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>()?.Name
                ?? char.ToLowerInvariant(prop.Name[0]) + prop.Name[1..];
            if (!jsonProperties.Contains(jsonName)) continue;
            if (prop.GetValue(activity) is null) unpopulated.Add(prop.Name);
        }

        Assert.Empty(unpopulated);
    }

    [Fact]
    public void KnownInvokeName_StillDeserializesAsTypedSubclass()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Invokes/HandoffActivity.json");

        var activity = JsonSerializer.Deserialize<InvokeActivity>(json);

        Assert.IsType<HandoffActivity>(activity);
    }
}
