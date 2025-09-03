// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Events;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Events;

public static partial class Event
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class ReadReceiptAttribute() : EventAttribute(Api.Activities.Events.Name.ReadReceipt)
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<ReadReceiptActivity>();
        public override bool Select(IActivity activity)
        {
            if (activity is ReadReceiptActivity)
            {
                return true;
            }

            return false;
        }
    }
}

public static partial class AppEventActivityExtensions
{
    public static App OnReadReceipt(this App app, Func<IContext<ReadReceiptActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Name = string.Join("/", [ActivityType.Event, Name.ReadReceipt]),
            Handler = async context =>
            {
                await handler(context.ToActivityType<ReadReceiptActivity>());
                return null;
            },
            Selector = activity => activity is ReadReceiptActivity
        });

        return app;
    }
}