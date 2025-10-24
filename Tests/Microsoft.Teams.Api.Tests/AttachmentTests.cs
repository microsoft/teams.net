using System.Text.Json;

namespace Microsoft.Teams.Api.Tests;

public class AttachmentTests
{
    [Fact]
    public void Layout_List_ShouldSerializeCorrectly()
    {
        var layout = Attachment.Layout.List;
        var json = JsonSerializer.Serialize(layout);

        Assert.Equal("\"list\"", json);
    }

    [Fact]
    public void Layout_Grid_ShouldSerializeCorrectly()
    {
        var layout = Attachment.Layout.Grid;
        var json = JsonSerializer.Serialize(layout);

        Assert.Equal("\"grid\"", json);
    }

    [Fact]
    public void Layout_List_ShouldDeserializeCorrectly()
    {
        var json = "\"list\"";
        var layout = JsonSerializer.Deserialize<Attachment.Layout>(json);

        Assert.NotNull(layout);
        Assert.True(layout.IsList);
        Assert.False(layout.IsGrid);
    }

    [Fact]
    public void Layout_Grid_ShouldDeserializeCorrectly()
    {
        var json = "\"grid\"";
        var layout = JsonSerializer.Deserialize<Attachment.Layout>(json);

        Assert.NotNull(layout);
        Assert.True(layout.IsGrid);
        Assert.False(layout.IsList);
    }

    [Fact]
    public void Layout_List_ValueShouldMatch()
    {
        var layout = Attachment.Layout.List;
        Assert.Equal("list", layout.Value);
    }

    [Fact]
    public void Layout_Grid_ValueShouldMatch()
    {
        var layout = Attachment.Layout.Grid;
        Assert.Equal("grid", layout.Value);
    }
}