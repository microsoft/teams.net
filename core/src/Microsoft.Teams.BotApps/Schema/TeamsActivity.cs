// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Bot.Core.Schema;
using Microsoft.Teams.BotApps.Schema.Entities;

namespace Microsoft.Teams.BotApps.Schema;

/// <summary>
/// Teams Activity schema.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227: Collection Properties should be read only", Justification = "<Pending>")]
public class TeamsActivity : CoreActivity
{
    /// <summary>
    /// Creates a new instance of the TeamsActivity class from the specified Activity object.
    /// </summary>
    /// <param name="activity">The Activity instance to convert. Cannot be null.</param>
    /// <returns>A TeamsActivity object that represents the specified Activity.</returns>
    public static TeamsActivity FromActivity(CoreActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);
        return new(activity);
    }

    /// <summary>
    /// Creates a new instance of the TeamsActivity class from the specified Activity object.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public static new TeamsActivity FromJsonString(string json)
         => JsonSerializer.Deserialize(json, TeamsActivityJsonContext.Default.TeamsActivity)!;

    /// <summary>
    /// Default constructor.
    /// </summary>
    [JsonConstructor]
    public TeamsActivity()
    {
        From = new TeamsConversationAccount(new ConversationAccount());
        Recipient = new TeamsConversationAccount(new ConversationAccount());
        Conversation = new TeamsConversation(new Conversation());

    }

    private TeamsActivity(CoreActivity activity)
    {
        Id = activity.Id;
        ServiceUrl = activity.ServiceUrl;
        ChannelId = activity.ChannelId;
        Type = activity.Type;
        // ReplyToId = activity.ReplyToId;
        Text = activity.Text;
        Entities = EntityList.FromJsonArray(activity.Entities);
        ChannelData = new TeamsChannelData(activity.ChannelData!);
        From = new TeamsConversationAccount(activity.From!);
        Recipient = new TeamsConversationAccount(activity.Recipient!);
        Conversation = new TeamsConversation(activity.Conversation!);
        Attachments = TeamsAttachment.FromJArray(activity.Attachments);

        //base.Entities = Entities.ToJsonArray();
        Rebase();
    }

    /// <summary>
    /// resets shadow properties in base class
    /// </summary>
    /// <returns></returns>
    internal TeamsActivity Rebase()
    {
        base.Entities = this.Entities?.ToJsonArray();
        base.ChannelData = this.ChannelData;
        base.From = this.From;
        base.Recipient = this.Recipient;
        base.Conversation = this.Conversation;

        return this;
    }

    /// <summary>
    /// Gets or sets the account information for the sender of the Teams conversation.
    /// </summary>
    [JsonPropertyName("from")] public new TeamsConversationAccount From { get; set; }

    /// <summary>
    /// Gets or sets the account information for the recipient of the Teams conversation.
    /// </summary>
    [JsonPropertyName("recipient")] public new TeamsConversationAccount Recipient { get; set; }

    /// <summary>
    /// Gets or sets the conversation information for the Teams conversation.
    /// </summary>
    [JsonPropertyName("conversation")] public new TeamsConversation Conversation { get; set; }

    /// <summary>
    /// Gets or sets the Teams-specific channel data associated with this activity.
    /// </summary>
    [JsonPropertyName("channelData")] public new TeamsChannelData? ChannelData { get; set; }

    /// <summary>
    /// Gets or sets the entities specific to Teams.
    /// </summary>
    [JsonPropertyName("entities")] public new EntityList? Entities { get; set; }

    /// <summary>
    /// Attachments specific to Teams.
    /// </summary>
    [JsonPropertyName("attachments")] public new IList<TeamsAttachment>? Attachments { get; set; }

    /// <summary>
    /// Adds an entity to the activity's Entities collection.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public TeamsActivity AddEntity(Entity entity)
    {
        Entities ??= [];
        Entities.Add(entity);
        return this;
    }

    /// <summary>
    /// Creates a new TeamsActivityBuilder instance for building a TeamsActivity with a fluent API.
    /// </summary>
    /// <returns>A new TeamsActivityBuilder instance.</returns>
    public static TeamsActivityBuilder CreateBuilder()
    {
        return new TeamsActivityBuilder();
    }

    /// <summary>
    /// Creates a new TeamsActivityBuilder instance initialized with this activity.
    /// </summary>
    /// <returns>A new TeamsActivityBuilder instance initialized with this activity.</returns>
    public TeamsActivityBuilder ToBuilder()
    {
        return new TeamsActivityBuilder(this);
    }
}
