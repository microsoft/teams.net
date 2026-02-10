using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Api.Tests.Activities;

public class MessageActivityTests
{
    [Fact]
    public void JsonSerialize()
    {
        var activity = new MessageActivity("testing123")
        {
            Id = "1",
            From = new()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new()
            {
                Id = "1",
                Type = ConversationType.Personal
            },
            Recipient = new()
            {
                Id = "2",
                Name = "test-bot",
                Role = Role.Bot
            }
        };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 4,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Message/MessageActivity.json"
        ), json);
    }

    [Fact]
    public void JsonSerialize_Derived()
    {
        MessageActivity activity = new MessageActivity("testing123")
        {
            Id = "1",
            From = new()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new()
            {
                Id = "1",
                Type = ConversationType.Personal
            },
            Recipient = new()
            {
                Id = "2",
                Name = "test-bot",
                Role = Role.Bot
            }
        };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 4,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Message/MessageActivity.json"
        ), json);
    }

    [Fact]
    public void JsonSerialize_Derived_Interface()
    {
        Activity activity = new MessageActivity("testing123")
        {
            Id = "1",
            From = new()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new()
            {
                Id = "1",
                Type = ConversationType.Personal
            },
            Recipient = new()
            {
                Id = "2",
                Name = "test-bot",
                Role = Role.Bot
            }
        };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 4,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        Assert.Equal(File.ReadAllText(
            @"../../../Json/Activity/Message/MessageActivity.json"
        ), json);
    }

    [Fact]
    public void JsonSerialize_Mention()
    {
        Account bot = new()
        {
            Id = "2",
            Name = "test-bot",
            Role = Role.Bot
        };

        Activity activity = new MessageActivity("testing123")
        {
            Id = "1",
            From = new()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new()
            {
                Id = "1",
                Type = ConversationType.Personal
            },
            Recipient = bot
        }.AddMention(bot);

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 4,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        var text = File.ReadAllText(
            @"../../../Json/Activity/Message/MessageActivity_Mention.json",
            Encoding.UTF8
        );

        Assert.Equal(text, json);
    }

    [Fact]
    public void JsonDeserialize()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Message/MessageActivity.json");
        var activity = JsonSerializer.Deserialize<MessageActivity>(json);
        var expected = new MessageActivity("testing123")
        {
            Id = "1",
            From = new()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new()
            {
                Id = "1",
                Type = ConversationType.Personal
            },
            Recipient = new()
            {
                Id = "2",
                Name = "test-bot",
                Role = Role.Bot
            }
        };

        Assert.Equivalent(expected, activity);
    }

    [Fact]
    public void JsonDeserialize_Derived()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Message/MessageActivity.json");
        var activity = JsonSerializer.Deserialize<Activity>(json);
        var expected = new MessageActivity("testing123")
        {
            Id = "1",
            From = new()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new()
            {
                Id = "1",
                Type = ConversationType.Personal
            },
            Recipient = new()
            {
                Id = "2",
                Name = "test-bot",
                Role = Role.Bot
            }
        };

        Assert.Equivalent(expected, activity);
    }

    [Fact]
    public void JsonSerialize_WebChat_AllowsEmptyConversationType()
    {
        MessageActivity activity = new MessageActivity()
        {
            Id = "1",
            ChannelId = new ChannelId("webchat"),
            From = new Account()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new Api.Conversation()
            {
                Id = "1"
            },
            Recipient = new Account
            {
                Id = "2",
                Name = "test-bot",
                Role = Role.Bot
            }
        };
        string json = JsonSerializer.Serialize(activity, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
        string expected = File.ReadAllText(@"../../../Json/Activity/Message/MessageActivity_webChat.json");
        Assert.Equal(expected, json);
    }

    [Fact]
    public void JsonDeserialize_WebChat_AllowsEmptyConversationType()
    {
        var json = File.ReadAllText(@"../../../Json/Activity/Message/MessageActivity_webChat.json");
        var activity = JsonSerializer.Deserialize<MessageActivity>(json);
        Assert.NotNull(activity);
        Assert.Equal("1", activity.Id);
        Assert.Equal("webchat", activity.ChannelId);
        Assert.NotNull(activity.From);
        Assert.Equal("1", activity.From.Id);
        Assert.Equal("test", activity.From.Name);
        Assert.NotNull(activity.From.Role);
        Assert.Equal(Role.User, activity.From.Role.Value);
        Assert.NotNull(activity.Conversation);
        Assert.Equal("1", activity.Conversation.Id);
        Assert.Null(activity.Conversation.Type);
        Assert.NotNull(activity.Recipient);
        Assert.Equal("2", activity.Recipient.Id);
        Assert.Equal("test-bot", activity.Recipient.Name);
        Assert.NotNull(activity.Recipient.Role);
        Assert.Equal(Role.Bot, activity.Recipient.Role.Value);
    }

    [Fact]
    public void MultipleAddAIGenerated_DoesNotCreateDuplicateMessageEntities()
    {
        var activity = new MessageActivity("test");

        activity.AddAIGenerated();
        activity.AddAIGenerated();
        activity.AddAIGenerated();

        var messageEntities = activity.Entities?.Where(e =>
            e.Type == "https://schema.org/Message" && e.OType == "Message"
        ).ToList();

        Assert.NotNull(messageEntities);
        Assert.Single(messageEntities);
    }

    [Fact]
    public void MultipleAddCitation_DoesNotCreateDuplicateMessageEntities()
    {
        var activity = new MessageActivity("test");

        var appearance1 = new Microsoft.Teams.Api.Entities.CitationAppearance
        {
            Name = "Doc 1",
            Abstract = "Abstract 1"
        };

        var appearance2 = new Microsoft.Teams.Api.Entities.CitationAppearance
        {
            Name = "Doc 2",
            Abstract = "Abstract 2"
        };

        var appearance3 = new Microsoft.Teams.Api.Entities.CitationAppearance
        {
            Name = "Doc 3",
            Abstract = "Abstract 3"
        };

        activity.AddCitation(1, appearance1);
        activity.AddCitation(2, appearance2);
        activity.AddCitation(3, appearance3);

        var messageEntities = activity.Entities?.Where(e =>
            e.Type == "https://schema.org/Message" && e.OType == "Message"
        ).ToList();

        Assert.NotNull(messageEntities);
        Assert.Single(messageEntities);

        // Verify all citations are in the single message entity
        var citationEntity = messageEntities[0] as Microsoft.Teams.Api.Entities.CitationEntity;
        Assert.NotNull(citationEntity);
        Assert.NotNull(citationEntity.Citation);
        Assert.Equal(3, citationEntity.Citation.Count);
    }

    [Fact]
    public void MixedAddAIGeneratedAndAddCitation_DoesNotCreateDuplicateMessageEntities()
    {
        var activity = new MessageActivity("test");

        activity.AddAIGenerated();

        var appearance = new Microsoft.Teams.Api.Entities.CitationAppearance
        {
            Name = "Doc 1",
            Abstract = "Abstract 1"
        };

        activity.AddCitation(1, appearance);
        activity.AddAIGenerated();

        var messageEntities = activity.Entities?.Where(e =>
            e.Type == "https://schema.org/Message" && e.OType == "Message"
        ).ToList();

        Assert.NotNull(messageEntities);
        Assert.Single(messageEntities);

        // Verify it has both AI generated flag and citation
        var entity = messageEntities[0] as Microsoft.Teams.Api.Entities.IMessageEntity;
        Assert.NotNull(entity?.AdditionalType);
        Assert.Contains("AIGeneratedContent", entity.AdditionalType);

        var citationEntity = messageEntities[0] as Microsoft.Teams.Api.Entities.CitationEntity;
        Assert.NotNull(citationEntity);
        Assert.NotNull(citationEntity.Citation);
        Assert.Single(citationEntity.Citation);
    }

    [Fact]
    public void AddMention_WithAddTextFalse_DoesNotAddTextToActivity()
    {
        Account bot = new()
        {
            Id = "2",
            Name = "test-bot",
            Role = Role.Bot
        };

        var activity = new MessageActivity("testing123")
        {
            Id = "1",
            From = new()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new()
            {
                Id = "1",
                Type = ConversationType.Personal
            },
            Recipient = bot
        }.AddMention(bot, addText: false);

        // Text should remain unchanged
        Assert.Equal("testing123", activity.Text);

        // But mention entity should still be added
        var mention = activity.GetAccountMention("2");
        Assert.NotNull(mention);
        Assert.Equal("<at>test-bot</at>", mention.Text);
    }

    [Fact]
    public void AddMention_WithCustomText_UsesCustomText()
    {
        Account bot = new()
        {
            Id = "2",
            Name = "test-bot",
            Role = Role.Bot
        };

        var activity = new MessageActivity("Hello ")
        {
            Id = "1",
            From = new()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new()
            {
                Id = "1",
                Type = ConversationType.Personal
            },
            Recipient = bot
        }.AddMention(bot, text: "Custom Name");

        // Text should include custom mention text at the beginning
        Assert.Equal("<at>Custom Name</at> Hello ", activity.Text);

        // Mention entity should use custom text
        var mention = activity.GetAccountMention("2");
        Assert.NotNull(mention);
        Assert.Equal("<at>Custom Name</at>", mention.Text);
    }

    [Fact]
    public void AddMention_WithCustomTextAndAddTextFalse_DoesNotAddText()
    {
        Account bot = new()
        {
            Id = "2",
            Name = "test-bot",
            Role = Role.Bot
        };

        var activity = new MessageActivity("Hello")
        {
            Id = "1",
            From = new()
            {
                Id = "1",
                Name = "test",
                Role = Role.User
            },
            Conversation = new()
            {
                Id = "1",
                Type = ConversationType.Personal
            },
            Recipient = bot
        }.AddMention(bot, text: "Custom Name", addText: false);

        // Text should remain unchanged
        Assert.Equal("Hello", activity.Text);

        // But mention entity should use custom text
        var mention = activity.GetAccountMention("2");
        Assert.NotNull(mention);
        Assert.Equal("<at>Custom Name</at>", mention.Text);
    }

    [Fact]
    public void WithTargetedRecipient_Bool_SetsIsTargeted()
    {
        var activity = new MessageActivity("hello").WithTargetedRecipient(true);

        Assert.True(activity.IsTargeted);
        Assert.Null(activity.Recipient);
    }

    [Fact]
    public void WithTargetedRecipient_String_SetsIsTargetedAndRecipient()
    {
        var activity = new MessageActivity("hello").WithTargetedRecipient("user-123");

        Assert.True(activity.IsTargeted);
        Assert.NotNull(activity.Recipient);
        Assert.Equal("user-123", activity.Recipient.Id);
        Assert.Equal(string.Empty, activity.Recipient.Name);
        Assert.Equal(Role.User, activity.Recipient.Role);
    }

    [Fact]
    public void WithTargetedRecipient_IsChainable()
    {
        var activity = new MessageActivity("hello")
            .WithImportance(Importance.High)
            .WithTargetedRecipient("user-456")
            .WithDeliveryMode(DeliveryMode.Notification);

        Assert.Equal("hello", activity.Text);
        Assert.Equal(Importance.High, activity.Importance);
        Assert.Equal(DeliveryMode.Notification, activity.DeliveryMode);
        Assert.True(activity.IsTargeted);
        Assert.NotNull(activity.Recipient);
        Assert.Equal("user-456", activity.Recipient.Id);
    }

    [Fact]
    public void JsonSerialize_WithIsTargeted()
    {
        var activity = new MessageActivity("targeted message").WithTargetedRecipient(true);
        activity.Id = "1";
        activity.From = new() { Id = "1", Name = "test", Role = Role.User };
        activity.Conversation = new() { Id = "1", Type = ConversationType.Personal };

        var json = JsonSerializer.Serialize(activity, new JsonSerializerOptions()
        {
            WriteIndented = true,
            IndentSize = 4,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        // IsTargeted is not serialized (JsonIgnore) - it's only used internally for SDK routing
        Assert.DoesNotContain("\"isTargeted\"", json);
        Assert.Contains("\"text\": \"targeted message\"", json);
        // Verify the property is still set on the object
        Assert.True(activity.IsTargeted);
    }

    [Fact]
    public void Validate_FluentAPI()
    {
        var msg = new MessageActivity("Hello")
            .WithTargetedRecipient("user-123")
            .WithDeliveryMode(DeliveryMode.Notification)
            .WithRecipient(new Account() { Id = "user-123", Name = "Test User", Role = Role.User })
            .WithImportance(Importance.High); 

        Assert.Equal("Hello", msg.Text);
        Assert.True(msg.IsTargeted);
        Assert.NotNull(msg.Recipient);
        Assert.Equal("user-123", msg.Recipient.Id);
        Assert.Equal("Test User", msg.Recipient.Name);
        Assert.Equal(Role.User, msg.Recipient.Role);
    }


    [Fact]
    public void Validate_FluentAPI_With_Different_Recipient()
    {
        var msg = new MessageActivity("Hello")
            .WithRecipient(new Account() { Id = "user-999", Name = "Another User", Role = Role.User })
            .WithTargetedRecipient("user-123")
            .WithDeliveryMode(DeliveryMode.Notification)
            .WithImportance(Importance.High);

        Assert.Equal("Hello", msg.Text);
        Assert.True(msg.IsTargeted);
        Assert.NotNull(msg.Recipient);
        Assert.Equal("user-123", msg.Recipient.Id);
        Assert.Equal("Test User", msg.Recipient.Name);
        Assert.Equal(Role.User, msg.Recipient.Role);
    }
}