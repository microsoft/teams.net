// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities;

public static partial class Conversation
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class UpdateAttribute : ActivityAttribute
    {
        public UpdateAttribute() : base(ActivityType.ConversationUpdate, typeof(ConversationUpdateActivity))
        {

        }

        public UpdateAttribute(ConversationUpdateActivity.EventType eventType) : base(string.Join("/", [ActivityType.ConversationUpdate, eventType]), typeof(ConversationUpdateActivity))
        {

        }

        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<ConversationUpdateActivity>();
    }
}

public static partial class AppActivityExtensions
{
    public static App OnConversationUpdate(this App app, Func<IContext<ConversationUpdateActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = ActivityType.ConversationUpdate,
            Handler = async context =>
            {
                await handler(context.ToActivityType<ConversationUpdateActivity>());
                return null;
            },
            Selector = activity => activity is ConversationUpdateActivity
        });

        return app;
    }
}