

using Microsoft.Teams.Api.MessageExtensions;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Annotations;


namespace Samples.MessageExtensions;
public class HomeController 
{
    [Message]
    public async Task OnMessage([Context] Microsoft.Teams.Api.Activities.MessageActivity activity, [Context] IContext.Client client)
    {
        await client.Send($"you said \"{activity.Text}\"");
    }

    [MessageExtension.SubmitAction]
    public Task<Microsoft.Teams.Api.MessageExtensions.Response?> OnSubmitAction([Context] Microsoft.Teams.Api.Activities.Invokes.MessageExtensions.SubmitActionActivity action, [Context] IContext.Client client)
    {
        var commanndId = action.Value.CommandId;

        if (string.IsNullOrEmpty(commanndId))
        {
            // context.Log.Error("No command ID provided for submit action.");
            return Task.FromResult<Microsoft.Teams.Api.MessageExtensions.Response?>(null);
        }

        if (commanndId == "createCard")
        {
            // context.Log.Info("createCard response to submit action.");
            // todo: create card
        }
        else if (commanndId == "getMessageDetails" && action.Value.MessagePayload is not null)
        {
            // context.Log.Info("getMessageDetails response to submit action.");
            // TODO: create message details card
        }
        else
        {
            // context.Log.Error($"Unknown command ID: {commanndId}");
            return Task.FromResult<Microsoft.Teams.Api.MessageExtensions.Response?>(null);
        }

        return Task.FromResult<Microsoft.Teams.Api.MessageExtensions.Response?>(new Microsoft.Teams.Api.MessageExtensions.Response()
        {
            ComposeExtension = new Result()
            {
                Attachments = new List<Attachment>()
                {
                    new Attachment()
                    {
                        Content = "Your updated content here",
                        Name = "Your updated attachment name",
                    }
                },
                AttachmentLayout = Microsoft.Teams.Api.Attachment.Layout.List,
            }
        });
    }
}
