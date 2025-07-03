// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

public static partial class MessageExtension
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true)]
    public class SettingAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.MessageExtensions.Setting, typeof(MessageExtensions.SettingActivity))
    {
        public override object Coerce(IContext<IActivity> context) => context.ToActivityType<MessageExtensions.SettingActivity>();
    }
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnSetting(this App app, Func<IContext<MessageExtensions.SettingActivity>, Task> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = async context =>
            {
                await handler(context.ToActivityType<MessageExtensions.SettingActivity>());
                return null;
            },
            Selector = activity => activity is MessageExtensions.SettingActivity
        });

        return app;
    }
}