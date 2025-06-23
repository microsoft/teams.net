// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps.Routing;

namespace Microsoft.Teams.Apps.Activities.Invokes;

[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public class FileConsentAttribute() : InvokeAttribute(Api.Activities.Invokes.Name.FileConsent, typeof(FileConsentActivity))
{
    public override object Coerce(IContext<IActivity> context) => context.ToActivityType<FileConsentActivity>();
}

public static partial class AppInvokeActivityExtensions
{
    public static App OnFileConsent(this App app, Func<IContext<FileConsentActivity>, Task<object?>> handler)
    {
        app.Router.Register(new Route()
        {
            Handler = context => handler(context.ToActivityType<FileConsentActivity>()),
            Selector = activity => activity is FileConsentActivity
        });

        return app;
    }
}