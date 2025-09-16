using System.Text.Json;

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
        app.AddTab("dialog-form", "Web/dialog-form");
        app.Run();
    }

    [TeamsController]
    public class Controller
    {
        private readonly IConfiguration _configuration;

        public Controller(IConfiguration configuration)
        {
            _configuration = configuration;
        }

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

            var data = activity.Value?.Data as JsonElement?;
            if (data == null)
            {
                log.Info("[TASK_FETCH] No data found in the activity value");
                return new Microsoft.Teams.Api.TaskModules.Response(new Microsoft.Teams.Api.TaskModules.MessageTask("No data found in the activity value"));
            }

            var dialogType = data.Value.TryGetProperty("opendialogtype", out var dialogTypeElement) && dialogTypeElement.ValueKind == JsonValueKind.String
                ? dialogTypeElement.GetString()
                : null;

            log.Info($"[TASK_FETCH] Dialog type: {dialogType}");

            return dialogType switch
            {
                "simple_form" => CreateSimpleFormDialog(),
                "webpage_dialog" => CreateWebpageDialog(_configuration, log),
                "multi_step_form" => CreateMultiStepFormDialog(),
                "mixed_example" => CreateMixedExampleDialog(),
                _ => new Microsoft.Teams.Api.TaskModules.Response(new Microsoft.Teams.Api.TaskModules.MessageTask("Unknown dialog type"))
            };
        }

        [TaskSubmit]
        public async Task<Microsoft.Teams.Api.TaskModules.Response> OnTaskSubmit([Context] Tasks.SubmitActivity activity, [Context] IContext.Client client, [Context] Microsoft.Teams.Common.Logging.ILogger log)
        {
            log.Info("[TASK_SUBMIT] Task submit request received");

            var data = activity.Value?.Data as JsonElement?;
            if (data == null)
            {
                log.Info("[TASK_SUBMIT] No data found in the activity value");
                return new Microsoft.Teams.Api.TaskModules.Response(new Microsoft.Teams.Api.TaskModules.MessageTask("No data found in the activity value"));
            }

            var submissionType = data.Value.TryGetProperty("submissiondialogtype", out var submissionTypeObj) && submissionTypeObj.ValueKind == JsonValueKind.String
                ? submissionTypeObj.ToString()
                : null;

            log.Info($"[TASK_SUBMIT] Submission type: {submissionType}");

            string? GetFormValue(string key)
            {
                if (data.Value.TryGetProperty(key, out var val))
                {
                    if (val is System.Text.Json.JsonElement element)
                        return element.GetString();
                    return val.ToString();
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

                case "webpage_dialog_step_1":
                    var nameStep1 = GetFormValue("name") ?? "Unknown";
                    var nextStepCardJson = $$"""
                    {
                        "type": "AdaptiveCard",
                        "version": "1.4",
                        "body": [
                            {
                                "type": "TextBlock", 
                                "text": "Email", 
                                "size": "Large", 
                                "weight": "Bolder"
                            },
                            {
                                "type": "Input.Text",
                                "id": "email",
                                "label": "Email",
                                "placeholder": "Enter your email",
                                "isRequired": true
                            }
                        ],
                        "actions": [
                            {
                                "type": "Action.Submit", 
                                "title": "Submit", 
                                "data": {"submissiondialogtype": "webpage_dialog_step_2", "name": "{{nameStep1}}"}
                            }
                        ]
                    }
                    """;

                    var nextStepCard = JsonSerializer.Deserialize<Microsoft.Teams.Cards.AdaptiveCard>(nextStepCardJson)
                        ?? throw new InvalidOperationException("Failed to deserialize next step card");

                    var nextStepTaskInfo = new Microsoft.Teams.Api.TaskModules.TaskInfo
                    {
                        Title = $"Thanks {nameStep1} - Get Email",
                        Card = new Microsoft.Teams.Api.Attachment
                        {
                            ContentType = new Microsoft.Teams.Api.ContentType("application/vnd.microsoft.card.adaptive"),
                            Content = nextStepCard
                        }
                    };

                    return new Microsoft.Teams.Api.TaskModules.Response(new Microsoft.Teams.Api.TaskModules.ContinueTask(nextStepTaskInfo));

                case "webpage_dialog_step_2":
                    var nameStep2 = GetFormValue("name") ?? "Unknown";
                    var emailStep2 = GetFormValue("email") ?? "No email";
                    await client.Send($"Hi {nameStep2}, thanks for submitting the form! We got that your email is {emailStep2}");
                    return new Microsoft.Teams.Api.TaskModules.Response(new Microsoft.Teams.Api.TaskModules.MessageTask("Multi-step form completed successfully"));

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
            // Create card from JSON similar to Python's model_validate approach
            var cardJson = """
            {
                "type": "AdaptiveCard",
                "version": "1.4",
                "body": [
                    {
                        "type": "TextBlock", 
                        "text": "This is a simple form", 
                        "size": "Large", 
                        "weight": "Bolder"
                    },
                    {
                        "type": "Input.Text",
                        "id": "name",
                        "label": "Name",
                        "placeholder": "Enter your name",
                        "isRequired": true
                    }
                ],
                "actions": [
                    {"type": "Action.Submit", "title": "Submit", "data": {"submissiondialogtype": "simple_form"}}
                ]
            }
            """;

            var dialogCard = JsonSerializer.Deserialize<Microsoft.Teams.Cards.AdaptiveCard>(cardJson)
                ?? throw new InvalidOperationException("Failed to deserialize simple form card");

            var serializedCard = JsonSerializer.Serialize(dialogCard);
            Console.WriteLine($"[DEBUG] Simple Form Card JSON: {serializedCard}");

            var taskInfo = new Microsoft.Teams.Api.TaskModules.TaskInfo
            {
                Title = "Simple Form Dialog",
                Card = new Microsoft.Teams.Api.Attachment
                {
                    ContentType = new Microsoft.Teams.Api.ContentType("application/vnd.microsoft.card.adaptive"),
                    Content = dialogCard
                }
            };

            var continueTask = new Microsoft.Teams.Api.TaskModules.ContinueTask(taskInfo);

            // Debug the ContinueTask before wrapping in Response
            Console.WriteLine($"[DEBUG] continueTask.Value is null: {continueTask.Value == null}");
            Console.WriteLine($"[DEBUG] continueTask.Value.Title: '{continueTask.Value?.Title}'");
            Console.WriteLine($"[DEBUG] continueTask.Value.Card is null: {continueTask.Value?.Card == null}");

            var debugOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
                WriteIndented = true
            };
            var continueTaskJson = JsonSerializer.Serialize(continueTask, debugOptions);
            Console.WriteLine($"[DEBUG] ContinueTask JSON (no ignore): {continueTaskJson}");

            var response = new Microsoft.Teams.Api.TaskModules.Response(continueTask);
            var serializedResponse = JsonSerializer.Serialize(response, debugOptions);
            Console.WriteLine($"[DEBUG] Response JSON (no ignore): {serializedResponse}");

            return response;
        }

        private static Microsoft.Teams.Api.TaskModules.Response CreateWebpageDialog(IConfiguration configuration, Microsoft.Teams.Common.Logging.ILogger log)
        {
            var botEndpoint = configuration["BotEndpoint"];
            if (string.IsNullOrEmpty(botEndpoint))
            {
                log.Warn("No remote endpoint detected. Using webpages for dialog will not work as expected");
                botEndpoint = "http://localhost:3978"; // Fallback for local development
            }
            else
            {
                log.Info($"Using BotEndpoint: {botEndpoint}/tabs/dialog-form");
            }

            var taskInfo = new Microsoft.Teams.Api.TaskModules.TaskInfo
            {
                Title = "Webpage Dialog",
                Width = new Union<int, Microsoft.Teams.Api.TaskModules.Size>(1000),
                Height = new Union<int, Microsoft.Teams.Api.TaskModules.Size>(800),
                Url = $"{botEndpoint}/tabs/dialog-form"
            };

            return new Microsoft.Teams.Api.TaskModules.Response(new Microsoft.Teams.Api.TaskModules.ContinueTask(taskInfo));
        }

        private static Microsoft.Teams.Api.TaskModules.Response CreateMultiStepFormDialog()
        {
            var cardJson = """
            {
                "type": "AdaptiveCard",
                "version": "1.4",
                "body": [
                    {
                        "type": "TextBlock", 
                        "text": "This is a multi-step form", 
                        "size": "Large", 
                        "weight": "Bolder"
                    },
                    {
                        "type": "Input.Text",
                        "id": "name",
                        "label": "Name",
                        "placeholder": "Enter your name",
                        "isRequired": true
                    }
                ],
                "actions": [
                    {
                        "type": "Action.Submit", 
                        "title": "Submit", 
                        "data": {"submissiondialogtype": "webpage_dialog_step_1"}
                    }
                ]
            }
            """;

            var dialogCard = JsonSerializer.Deserialize<Microsoft.Teams.Cards.AdaptiveCard>(cardJson)
                ?? throw new InvalidOperationException("Failed to deserialize multi-step form card");

            var taskInfo = new Microsoft.Teams.Api.TaskModules.TaskInfo
            {
                Title = "Multi-step Form Dialog",
                Card = new Microsoft.Teams.Api.Attachment
                {
                    ContentType = new Microsoft.Teams.Api.ContentType("application/vnd.microsoft.card.adaptive"),
                    Content = dialogCard
                }
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