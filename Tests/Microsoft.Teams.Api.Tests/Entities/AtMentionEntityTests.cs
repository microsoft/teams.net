using System.Text.Json;

using Microsoft.Teams.Api.Entities;

namespace Microsoft.Teams.Api.Tests.Entities;

public class AtMentionEntityTests
{
    [Fact]
    public void AtMentionEntity_JsonSerialize()
    {
        var account = new Account() { Id = "accountId", Name = "acctName" };
        var entity = new MentionEntity()
        {
            Mentioned = account,
            Text = $"<at>{account.Name}</at>"
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/AtMentionEntity.json"
        ), json);
    }


    [Fact]
    public void AtMentionEntity_JsonSerialize_Derived()
    {
        var account = new Account() { Id = "accountId", Name = "acctName" };
        MentionEntity entity = new MentionEntity()
        {
            Mentioned = account,
            Text = $"<at>{account.Name}</at>"
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/AtMentionEntity.json"
        ), json);
    }

    [Fact]
    public void AtMentionEntity_JsonSerialize_Interface_Derived()
    {
        var account = new Account() { Id = "accountId", Name = "acctName" };
        IEntity entity = new MentionEntity()
        {
            Mentioned = account,
            Text = $"<at>{account.Name}</at>"
        };

        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 2,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Entities/AtMentionEntity.json"
        ), json);
    }


    [Fact]
    public void AtMentionEntity_JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/AtMentionEntity.json");
        var entity = JsonSerializer.Deserialize<MentionEntity>(json);
        var account = new Account() { Id = "accountId", Name = "acctName" };
        var expected = new MentionEntity()
        {
            Mentioned = account,
            Text = $"<at>{account.Name}</at>"
        };

        Assert.Equivalent(expected, entity);
    }

    [Fact]
    public void AtMentionEntity_JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Entities/AtMentionEntity.json");
        var entity = JsonSerializer.Deserialize<IEntity>(json);
        var account = new Account() { Id = "accountId", Name = "acctName" };
        var expected = new MentionEntity()
        {
            Mentioned = account,
            Text = $"<at>{account.Name}</at>"
        };

        Assert.Equivalent(expected, entity);
    }


}