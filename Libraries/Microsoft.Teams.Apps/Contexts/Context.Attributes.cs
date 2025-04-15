using System.Reflection;

using Microsoft.Teams.Api.Activities;

namespace Microsoft.Teams.Apps;

public partial interface IContext
{
    [Flags]
    public enum Property
    {
        None = 0,
        AppId = 1,
        Activity = 2,
        Ref = 4,
        Context = AppId | Activity | Ref,
    }

    /// <summary>
    /// the base for any context property attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public abstract class PropertyAttribute : Attribute
    {
        /// <summary>
        /// resolves the context property value
        /// </summary>
        public abstract object Resolve(IContext<IActivity> context, ParameterInfo parameter);
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class AppIdAttribute : PropertyAttribute
    {
        public override object Resolve(IContext<IActivity> context, ParameterInfo parameter) => context.AppId;
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class LoggerAttribute : PropertyAttribute
    {
        public override object Resolve(IContext<IActivity> context, ParameterInfo parameter) => context.Log;
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class StorageAttribute : PropertyAttribute
    {
        public override object Resolve(IContext<IActivity> context, ParameterInfo parameter) => context.Storage;
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class ApiAttribute : PropertyAttribute
    {
        public override object Resolve(IContext<IActivity> context, ParameterInfo parameter) => context.Api;
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class ActivityAttribute : PropertyAttribute
    {
        public override object Resolve(IContext<IActivity> context, ParameterInfo parameter)
        {
            return context.Activity.ToType(parameter.ParameterType, null);
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class RefAttribute : PropertyAttribute
    {
        public override object Resolve(IContext<IActivity> context, ParameterInfo parameter) => context.Ref;
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class UserGraphAttribute : PropertyAttribute
    {
        public override object Resolve(IContext<IActivity> context, ParameterInfo parameter) => context.UserGraph;
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class ClientAttribute : PropertyAttribute
    {
        public override object Resolve(IContext<IActivity> context, ParameterInfo parameter) => new Client(context);
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class IsSignedInAttribute : PropertyAttribute
    {
        public override object Resolve(IContext<IActivity> context, ParameterInfo parameter) => context.IsSignedIn;
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
    public class NextAttribute : PropertyAttribute
    {
        public override object Resolve(IContext<IActivity> context, ParameterInfo parameter)
        {
            return new Next(context.Next);
        }
    }

    /// <summary>
    /// an object that can send activities
    /// </summary>
    /// <param name="context">the parent context</param>
    public class Client(IContext<IActivity> context)
    {
        /// <summary>
        /// send an activity to the conversation
        /// </summary>
        /// <param name="activity">activity activity to send</param>
        public Task<T> Send<T>(T activity) where T : IActivity => context.Send(activity);

        /// <summary>
        /// send a message activity to the conversation
        /// </summary>
        /// <param name="text">the text to send</param>
        public Task<MessageActivity> Send(string text) => context.Send(text);

        /// <summary>
        /// send a message activity with a card attachment
        /// </summary>
        /// <param name="card">the card to send as an attachment</param>
        public Task<MessageActivity> Send(Cards.Card card) => context.Send(card);

        /// <summary>
        /// send a typing activity
        /// </summary>
        public Task<TypingActivity> Typing() => context.Typing();

        /// <summary>
        /// trigger user signin flow for the activity sender
        /// </summary>
        /// <param name="options">option overrides</param>
        /// <returns>the existing user token if found</returns>
        public Task<string?> SignIn(SignInOptions? options = null) => context.SignIn(options);

        /// <summary>
        /// trigger user signin flow for the activity sender
        /// </summary>
        /// <param name="connectionName">the connection name</param>
        public Task SignOut(string? connectionName = null) => context.SignOut(connectionName);
    }

    /// <summary>
    /// calls the next handler in the route chain
    /// </summary>
    public delegate Task<object?> Next();
}