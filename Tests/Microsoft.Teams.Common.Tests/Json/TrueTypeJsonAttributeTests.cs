
using System.Text.Json;

using Microsoft.Teams.Common.Json;
using Microsoft.Teams.Plugins.AspNetCore.DevTools;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Events;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Models;

namespace Microsoft.Teams.Common.Tests.Json;
public class TrueTypeJsonAttributeTests
{
    // Test that the attribute can be applied and sets the correct converter type
    [Fact]
    public void TrueTypeJsonAttribute_SetsConverterType()
    {
        var attr = new TrueTypeJsonAttribute<string>();
        Assert.Equal(typeof(TrueTypeJsonConverter<string>), attr.ConverterType);
    }

    [Fact]
    public void TrueTypeJsonConverter_Serialize()
    {
        // Arrange
        MetaData body = new MetaData();
        body.Id = "bodyGuid";
        body.Name = "MetaDataName";
        var metaDataEvent = new MetaDataEvent(body);
        Assert.True(metaDataEvent is MetaDataEvent);
        Assert.Equal("metadata", metaDataEvent.Type);

        // Act
        var json = JsonSerializer.Serialize(metaDataEvent);
        // Assert
        Assert.Contains("\"id\":\"bodyGuid\"", json);
        Assert.Contains("\"name\":\"MetaDataName\"", json);
        Assert.Contains("\"type\":\"metadata\"", json);
        Assert.Contains("\"sentAt\":\"", json);

    }

    [Fact]
    public void TrueTypeJsonConverter_Deserialize()
    {
        // Arrange
        MetaData body = new MetaData();
        body.Id = "bodyGuid";
        body.Name = "MetaDataName";
        var metaDataEvent = new MetaDataEvent(body);
        var json = JsonSerializer.Serialize<IEvent>(metaDataEvent);

        // Act
        var ex = Assert.Throws<System.NotImplementedException>(() => JsonSerializer.Deserialize<IEvent>(json));
        // Assert
        var expectedSubmitException = "The method or operation is not implemented.";
        Assert.Equal(expectedSubmitException, ex.Message);
    }



}