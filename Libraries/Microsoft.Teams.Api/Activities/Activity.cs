// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Teams.Api.Entities;
using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities;

[JsonConverter(typeof(JsonConverter<ActivityType>))]
public partial class ActivityType(string value) : StringEnum(value)
{
    public Type ToType()
    {
        if (IsTyping) return typeof(TypingActivity);
        if (IsCommand) return typeof(CommandActivity);
        if (IsCommandResult) return typeof(CommandResultActivity);
        if (IsConversationUpdate) return typeof(ConversationUpdateActivity);
        if (IsEndOfConversation) return typeof(EndOfConversationActivity);
        if (IsInstallUpdate) return typeof(InstallUpdateActivity);
        if (IsMessage) return typeof(MessageActivity);
        if (IsMessageUpdate) return typeof(MessageUpdateActivity);
        if (IsMessageDelete) return typeof(MessageDeleteActivity);
        if (IsMessageReaction) return typeof(MessageReactionActivity);
        if (IsEvent) return typeof(EventActivity);
        if (IsInvoke) return typeof(InvokeActivity);
        return typeof(Activity);
    }

    public string ToPrettyString()
    {
        var value = ToString();
        return $"{value.First().ToString().ToUpper()}{value.AsSpan(1).ToString()}";
    }
}

[JsonConverter(typeof(ActivityJsonConverter))]
public partial interface IActivity : IConvertible, ICloneable
{
    public string Id { get; set; }

    public ActivityType Type { get; set; }

    public string? ReplyToId { get; set; }

    public ChannelId ChannelId { get; set; }

    public Account From { get; set; }

    public Account Recipient { get; set; }

    public Conversation Conversation { get; set; }

    public ConversationReference? RelatesTo { get; set; }

    public string? ServiceUrl { get; set; }

    public string? Locale { get; set; }

    public DateTime? Timestamp { get; set; }

    public DateTime? LocalTimestamp { get; set; }

    public IList<IEntity>? Entities { get; set; }

    public ChannelData? ChannelData { get; set; }

    public IDictionary<string, object?> Properties { get; set; }

    /// <summary>
    /// is this a streaming activity
    /// </summary>
    [JsonIgnore]
    public bool IsStreaming { get; }

    /// <summary>
    /// get the activity type/name path
    /// </summary>
    public string GetPath();

    /// <summary>
    /// get the quote reply string form of this activity
    /// </summary>
    public string ToQuoteReply();
}

[JsonConverter(typeof(ActivityJsonConverter))]
public partial class Activity : IActivity
{
    [JsonPropertyName("id")]
    [JsonPropertyOrder(0)]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    [JsonPropertyOrder(10)]
    public ActivityType Type { get; set; }

    [JsonPropertyName("replyToId")]
    [JsonPropertyOrder(20)]
    public string? ReplyToId { get; set; }

    [JsonPropertyName("channelId")]
    [JsonPropertyOrder(30)]
    public ChannelId ChannelId { get; set; } = ChannelId.MsTeams;

    [JsonPropertyName("from")]
    [JsonPropertyOrder(40)]
    public Account From { get; set; }

    [JsonPropertyName("recipient")]
    [JsonPropertyOrder(50)]
    public Account Recipient { get; set; }

    [JsonPropertyName("conversation")]
    [JsonPropertyOrder(60)]
    public Conversation Conversation { get; set; }

    [JsonPropertyName("relatesTo")]
    [JsonPropertyOrder(70)]
    public ConversationReference? RelatesTo { get; set; }

    [JsonPropertyName("serviceUrl")]
    [JsonPropertyOrder(80)]
    public string? ServiceUrl { get; set; }

    [JsonPropertyName("locale")]
    [JsonPropertyOrder(90)]
    public string? Locale { get; set; }

    [JsonPropertyName("timestamp")]
    [JsonPropertyOrder(100)]
    public DateTime? Timestamp { get; set; }

    [JsonPropertyName("localTimestamp")]
    [JsonPropertyOrder(110)]
    public DateTime? LocalTimestamp { get; set; }

    [JsonPropertyName("entities")]
    [JsonPropertyOrder(120)]
    public IList<IEntity>? Entities { get; set; }

    [JsonPropertyName("channelData")]
    [JsonPropertyOrder(130)]
    public ChannelData? ChannelData { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();

    [JsonConstructor]
    public Activity(string type)
    {
        Type = new(type);
    }

    public Activity(ActivityType type)
    {
        Type = type;
    }

    public Activity(IActivity activity)
    {
        Id = activity.Id;
        Type = activity.Type;
        ReplyToId = activity.ReplyToId;
        ChannelId = activity.ChannelId;
        From = activity.From;
        Recipient = activity.Recipient;
        Conversation = activity.Conversation;
        RelatesTo = activity.RelatesTo;
        ServiceUrl = activity.ServiceUrl;
        Locale = activity.Locale;
        Timestamp = activity.Timestamp;
        LocalTimestamp = activity.LocalTimestamp;
        Entities = activity.Entities;
        ChannelData = activity.ChannelData;
        Properties = activity.Properties;
    }

    [JsonIgnore]
    public bool IsStreaming => Entities?.Any(entity => entity.Type == "streaminfo" && entity is StreamInfoEntity) ?? false;

    public object Clone() => MemberwiseClone();
    public virtual Activity Copy() => (Activity)Clone();
    public virtual string GetPath() => string.Join(".", ["Activity", Type.ToPrettyString()]);

    public virtual Activity WithId(string value)
    {
        Id = value;
        return this;
    }

    public virtual Activity WithReplyToId(string value)
    {
        ReplyToId = value;
        return this;
    }

    public virtual Activity WithChannelId(ChannelId value)
    {
        ChannelId = value;
        return this;
    }

    public virtual Activity WithFrom(Account value)
    {
        From = value;
        return this;
    }

    public virtual Activity WithConversation(Conversation value)
    {
        Conversation = value;
        return this;
    }

    public virtual Activity WithRelatesTo(ConversationReference value)
    {
        RelatesTo = value;
        return this;
    }

    public virtual Activity WithRecipient(Account value)
    {
        Recipient = value;
        return this;
    }

    public virtual Activity WithServiceUrl(string value)
    {
        ServiceUrl = value;
        return this;
    }

    public virtual Activity WithTimestamp(DateTime value)
    {
        Timestamp = value;
        return this;
    }

    public virtual Activity WithLocale(string value)
    {
        Locale = value;
        return this;
    }

    public virtual Activity WithLocalTimestamp(DateTime value)
    {
        LocalTimestamp = value;
        return this;
    }

    public virtual Activity WithData(ChannelData value)
    {
        ChannelData ??= new();
        ChannelData.Merge(value);
        return this;
    }

    public virtual Activity WithData(string key, object? value)
    {
        ChannelData ??= new();
        ChannelData.Properties[key] = value;
        return this;
    }

    public virtual Activity WithAppId(string value)
    {
        ChannelData ??= new();
        ChannelData.App ??= new App() { Id = value };
        return this;
    }

    /// <summary>
    /// add an entity
    /// </summary>
    public virtual Activity AddEntity(params IEntity[] entities)
    {
        Entities ??= [];

        foreach (var entity in entities)
        {
            Entities.Add(entity);
        }

        return this;
    }

    public virtual Activity UpdateEntity(IEntity oldEntity, IEntity newEntity)
    {
        if (Entities != null)
        {
            Entities.Remove(oldEntity);
        }
        else
        {
            Entities = [];
        }

        Entities.Add(newEntity);
        return this;
    }

    /// <summary>
    /// ensures a single root level message entity exists
    /// </summary>
    private IMessageEntity GetRootLevelMessageEntity()
    {
        var messageEntity = Entities?.FirstOrDefault(
            e => e.Type == "https://schema.org/Message" && e.OType == "Message"
        ) as IMessageEntity;

        if (messageEntity is null)
        {
            messageEntity = new MessageEntity()
            {
                Type = "https://schema.org/Message",
                OType = "Message",
                OContext = "https://schema.org"
            };
            
            AddEntity(messageEntity);
        }

        return messageEntity;
    }

    /// <summary>
    /// add the `Generated By AI` label
    /// </summary>
    public virtual Activity AddAIGenerated()
    {
        var messageEntity = GetRootLevelMessageEntity();
        messageEntity.AdditionalType ??= [];

        if (!messageEntity.AdditionalType.Contains("AIGeneratedContent"))
        {
            messageEntity.AdditionalType.Add("AIGeneratedContent");
        }

        return this;
    }

    /// <summary>
    /// add content sensitivity label
    /// </summary>
    /// <param name="name">the content title</param>
    /// <param name="description">the content description</param>
    /// <param name="pattern">the pattern</param>
    public virtual Activity AddSensitivityLabel(string name, string? description = null, DefinedTerm? pattern = null)
    {
        return AddEntity(new SensitiveUsageEntity()
        {
            Name = name,
            Description = description,
            Pattern = pattern
        });
    }

    /// <summary>
    /// enable/disable message feedback
    /// </summary>
    public virtual Activity AddFeedback(bool value = true)
    {
        ChannelData ??= new();
        ChannelData.FeedbackLoopEnabled = value;
        return this;
    }

    /// <summary>
    /// add a citation
    /// </summary>
    public virtual Activity AddCitation(int position, CitationAppearance appearance)
    {
        var messageEntity = GetRootLevelMessageEntity();
        var citationEntity = new CitationEntity(messageEntity);
        citationEntity.Citation ??= [];
        citationEntity.Citation.Add(new CitationEntity.Claim()
        {
            Position = position,
            Appearance = appearance.ToDocument()
        });

        UpdateEntity(messageEntity, citationEntity);
        return this;
    }

    public CommandActivity ToCommand() => (CommandActivity)this;
    public CommandResultActivity ToCommandResult() => (CommandResultActivity)this;
    public TypingActivity ToTyping() => (TypingActivity)this;
    public InstallUpdateActivity ToInstallUpdate() => (InstallUpdateActivity)this;
    public MessageActivity ToMessage() => (MessageActivity)this;
    public MessageUpdateActivity ToMessageUpdate() => (MessageUpdateActivity)this;
    public MessageDeleteActivity ToMessageDelete() => (MessageDeleteActivity)this;
    public MessageReactionActivity ToMessageReaction() => (MessageReactionActivity)this;
    public ConversationUpdateActivity ToConversationUpdate() => (ConversationUpdateActivity)this;
    public EndOfConversationActivity ToEndOfConversation() => (EndOfConversationActivity)this;
    public EventActivity ToEvent() => (EventActivity)this;
    public InvokeActivity ToInvoke() => (InvokeActivity)this;

    public Activity Merge(Activity from)
    {
        Id ??= from.Id;
        ReplyToId ??= from.ReplyToId;
        ChannelId ??= from.ChannelId;
        From ??= from.From;
        Recipient ??= from.Recipient;
        Conversation ??= from.Conversation;
        RelatesTo ??= from.RelatesTo;
        ServiceUrl ??= from.ServiceUrl;
        Locale ??= from.Locale;
        Timestamp ??= from.Timestamp;
        LocalTimestamp ??= from.LocalTimestamp;
        AddEntity(from.Entities?.ToArray() ?? []);

        if (from.ChannelData is not null)
        {
            WithData(from.ChannelData);
        }

        if (from.Properties is not null)
        {
            Properties ??= new Dictionary<string, object?>();

            foreach (var kv in from.Properties)
            {
                Properties[kv.Key] = kv.Value;
            }
        }

        return this;
    }

    public string ToQuoteReply()
    {
        var text = string.Empty;

        if (this is MessageActivity message)
        {
            text = $"<p itemprop=\"preview\">{message.Text}</p>";
        }

        return $"""
        <blockquote itemscope="" itemtype="http://schema.skype.com/Reply" itemid="{Id}">
            <strong itemprop="mri" itemid="{From.Id}">
                {From.Name}
            </strong>
            <span itemprop="time" itemid="{Id}"></span>
            {text}
        </blockquote>
        """;
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}