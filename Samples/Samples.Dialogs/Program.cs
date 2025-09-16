using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Common;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

namespace Samples.Dialogs;

public static partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls("http://localhost:3978");
        builder.Services.AddOpenApi();
        builder.Services.AddTransient<Controller>();
        builder.AddTeams().AddTeamsDevTools();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseTeams();
        app.Run();
    }

    [TeamsController]
    public class Controller
    {

        [Message]
        public async Task OnMessage([Context] Microsoft.Teams.Api.Activities.MessageActivity activity, [Context] IContext.Client client, [Context] Microsoft.Teams.Common.Logging.ILogger log)
        {
            log.Info($"[MESSAGE] Received: {SanitizeForLog(activity.Text)}");
            log.Info($"[MESSAGE] From: {SanitizeForLog(activity.From?.Name ?? "unknown")}");

            // Create the launcher adaptive card
            var card = CreateDialogLauncherCard();
            await client.Send(card);
        }

        [TaskFetch]
        public Microsoft.Teams.Api.TaskModules.Response OnTaskFetch([Context] Tasks.FetchActivity activity, [Context] IContext.Client client, [Context] Microsoft.Teams.Common.Logging.ILogger log)
        {
            log.Info("[TASK_FETCH] Task fetch request received");

            var data = activity.Value?.Data as Dictionary<string, object>;
            var dialogType = data?.TryGetValue("opendialogtype", out var dialogTypeObj) == true
                ? dialogTypeObj?.ToString()
                : null;

            log.Info($"[TASK_FETCH] Dialog type: {dialogType}");

            return dialogType switch
            {
                "simple_form" => CreateSimpleFormDialog(),
                "webpage_dialog" => CreateWebpageDialog(),
                "multi_step_form" => CreateMultiStepFormDialog(),
                "mixed_example" => CreateMixedExampleDialog(),
                _ => new Microsoft.Teams.Api.TaskModules.Response(new Microsoft.Teams.Api.TaskModules.MessageTask("Unknown dialog type"))
            };
        }

        [TaskSubmit]
        public async Task<Microsoft.Teams.Api.TaskModules.Response> OnTaskSubmit([Context] Tasks.SubmitActivity activity, [Context] IContext.Client client, [Context] Microsoft.Teams.Common.Logging.ILogger log)
        {
            log.Info("[TASK_SUBMIT] Task submit request received");

            var data = activity.Value?.Data as Dictionary<string, object>;
            var submissionType = data?.TryGetValue("submissiondialogtype", out var submissionTypeObj) == true
                ? submissionTypeObj?.ToString()
                : null;

            log.Info($"[TASK_SUBMIT] Submission type: {submissionType}");

            string? GetFormValue(string key)
            {
                if (data?.TryGetValue(key, out var val) == true)
                {
                    if (val is System.Text.Json.JsonElement element)
                        return element.GetString();
                    return val?.ToString();
                }
                return null;
            }

            switch (submissionType)
            {
                case "simple_form":
                    var name = GetFormValue("name") ?? "Unknown";
                    await client.Send($"Hi {name}, thanks for submitting the form!");
                    return new Microsoft.Teams.Api.TaskModules.Response(new Microsoft.Teams.Api.TaskModules.MessageTask("Form was submitted"));

                case "webpage_dialog":
                    var webName = GetFormValue("name") ?? "Unknown";
                    var email = GetFormValue("email") ?? "No email";
                    await client.Send($"Hi {webName}, thanks for submitting the form! We got that your email is {email}");
                    return new Microsoft.Teams.Api.TaskModules.Response(new Microsoft.Teams.Api.TaskModules.MessageTask("Form submitted successfully"));

                default:
                    return new Microsoft.Teams.Api.TaskModules.Response(new Microsoft.Teams.Api.TaskModules.MessageTask("Unknown submission type"));
            }
        }

        private static Microsoft.Teams.Cards.AdaptiveCard CreateDialogLauncherCard()
        {
            var cardJson = """
            {
                "type": "AdaptiveCard",
                "version": "1.4",
                "body": [
                    {
                        "type": "TextBlock",
                        "text": "Select the examples you want to see!",
                        "size": "Large",
                        "weight": "Bolder"
                    }
                ],
                "actions": [
                    {
                        "type": "Action.Submit",
                        "title": "Simple form test",
                        "data": {
                            "msteams": {
                                "type": "task/fetch"
                            },
                            "opendialogtype": "simple_form"
                        }
                    },
                    {
                        "type": "Action.Submit",
                        "title": "Webpage Dialog",
                        "data": {
                            "msteams": {
                                "type": "task/fetch"
                            },
                            "opendialogtype": "webpage_dialog"
                        }
                    },
                    {
                        "type": "Action.Submit",
                        "title": "Multi-step Form",
                        "data": {
                            "msteams": {
                                "type": "task/fetch"
                            },
                            "opendialogtype": "multi_step_form"
                        }
                    },
                    {
                        "type": "Action.Submit",
                        "title": "Mixed Example",
                        "data": {
                            "msteams": {
                                "type": "task/fetch"
                            },
                            "opendialogtype": "mixed_example"
                        }
                    }
                ]
            }
            """;

            return System.Text.Json.JsonSerializer.Deserialize<Microsoft.Teams.Cards.AdaptiveCard>(cardJson)
                ?? throw new InvalidOperationException("Failed to deserialize launcher card");
        }

        private static Microsoft.Teams.Api.TaskModules.Response CreateSimpleFormDialog()
        {
            var taskInfo = new Microsoft.Teams.Api.TaskModules.TaskInfo
            {
                Title = "Simple Form Dialog (C# Sample)",
                Width = new Union<int, Microsoft.Teams.Api.TaskModules.Size>(500),
                Height = new Union<int, Microsoft.Teams.Api.TaskModules.Size>(400),
                Url = "https://teams.microsoft.com/l/task/example"
            };

            return new Microsoft.Teams.Api.TaskModules.Response(new Microsoft.Teams.Api.TaskModules.ContinueTask(taskInfo));
        }

        private static Microsoft.Teams.Api.TaskModules.Response CreateWebpageDialog()
        {
            var taskInfo = new Microsoft.Teams.Api.TaskModules.TaskInfo
            {
                Title = "Webpage Dialog (C# Sample)",
                Width = new Union<int, Microsoft.Teams.Api.TaskModules.Size>(1000),
                Height = new Union<int, Microsoft.Teams.Api.TaskModules.Size>(800),
                Url = "https://adaptivecards.io/designer/"
            };

            return new Microsoft.Teams.Api.TaskModules.Response(new Microsoft.Teams.Api.TaskModules.ContinueTask(taskInfo));
        }

        private static Microsoft.Teams.Api.TaskModules.Response CreateMultiStepFormDialog()
        {
            var taskInfo = new Microsoft.Teams.Api.TaskModules.TaskInfo
            {
                Title = "Multi-step Form Dialog (C# Sample)",
                Width = new Union<int, Microsoft.Teams.Api.TaskModules.Size>(600),
                Height = new Union<int, Microsoft.Teams.Api.TaskModules.Size>(500),
                Url = "https://teams.microsoft.com/l/task/example-multistep"
            };

            return new Microsoft.Teams.Api.TaskModules.Response(new Microsoft.Teams.Api.TaskModules.ContinueTask(taskInfo));
        }

        private static Microsoft.Teams.Api.TaskModules.Response CreateMixedExampleDialog()
        {
            var taskInfo = new Microsoft.Teams.Api.TaskModules.TaskInfo
            {
                Title = "Mixed Example (C# Sample)",
                Width = new Union<int, Microsoft.Teams.Api.TaskModules.Size>(800),
                Height = new Union<int, Microsoft.Teams.Api.TaskModules.Size>(600),
                Url = "https://teams.microsoft.com/l/task/example-mixed"
            };

            return new Microsoft.Teams.Api.TaskModules.Response(new Microsoft.Teams.Api.TaskModules.ContinueTask(taskInfo));
        }

        private static string SanitizeForLog(string? input)
        {
            if (input == null) return "";
            return input.Replace("\r", "").Replace("\n", "");
        }
    }
}