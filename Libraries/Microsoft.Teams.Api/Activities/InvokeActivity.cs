using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities;

public partial class ActivityType : StringEnum
{
    public static readonly ActivityType Invoke = new("invoke");
    public bool IsInvoke => Invoke.Equals(Value);
}

public interface IInvokeActivity
{
    /// <summary>
    /// The name of the operation associated with an invoke or event activity.
    /// </summary>
    public Invokes.Name Name { get; set; }

    /// <summary>
    /// A value that is associated with the activity.
    /// </summary>
    public object? Value { get; set; }
}

[JsonConverter(typeof(JsonConverter))]
public partial class InvokeActivity(Invokes.Name name) : Activity(ActivityType.Invoke), IInvokeActivity
{
    /// <summary>
    /// The name of the operation associated with an invoke or event activity.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonPropertyOrder(31)]
    public Invokes.Name Name { get; set; } = name;

    /// <summary>
    /// A value that is associated with the activity.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonPropertyOrder(32)]
    public object? Value { get; set; }

    public override string GetPath()
    {
        return string.Join('.', ["Activity", Type.ToPrettyString(), Name.ToPrettyString()]);
    }

    public Invokes.ExecuteActionActivity ToExecuteAction() => (Invokes.ExecuteActionActivity)this;
    public Invokes.FileConsentActivity ToFileConsent() => (Invokes.FileConsentActivity)this;
    public Invokes.HandoffActivity ToHandoff() => (Invokes.HandoffActivity)this;
    public Invokes.AdaptiveCardActivity ToAdaptiveCard() => (Invokes.AdaptiveCardActivity)this;
    public Invokes.ConfigActivity ToConfig() => (Invokes.ConfigActivity)this;
    public Invokes.MessageExtensionActivity ToMessageExtension() => (Invokes.MessageExtensionActivity)this;
    public new Invokes.MessageActivity ToMessage() => (Invokes.MessageActivity)this;
    public Invokes.SignInActivity ToSignIn() => (Invokes.SignInActivity)this;
    public Invokes.TabActivity ToTab() => (Invokes.TabActivity)this;
    public Invokes.TaskActivity ToTask() => (Invokes.TaskActivity)this;

    public override object ToType(Type type, IFormatProvider? provider)
    {
        if (type == Invokes.Name.ExecuteAction.ToType()) return ToExecuteAction();
        if (type == Invokes.Name.FileConsent.ToType()) return ToFileConsent();
        if (type == Invokes.Name.Handoff.ToType()) return ToHandoff();
        if (type == typeof(Invokes.AdaptiveCardActivity)) return ToAdaptiveCard();
        if (type == typeof(Invokes.ConfigActivity)) return ToConfig();
        if (type == typeof(Invokes.MessageExtensionActivity)) return ToMessageExtension();
        if (type == typeof(Invokes.MessageActivity)) return ToMessage();
        if (type == typeof(Invokes.SignInActivity)) return ToSignIn();
        if (type == typeof(Invokes.TabActivity)) return ToTab();
        if (type == typeof(Invokes.TaskActivity)) return ToTask();
        return this;
    }
}