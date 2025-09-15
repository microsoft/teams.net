using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.AdaptiveCards;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

namespace Samples.Cards;

public static partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // Bind the application to localhost:3978 when run with `dotnet run`
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
        [Activity]
        public async Task OnActivity(IContext<Activity> context, [Context] IContext.Next next)
        {
            context.Log.Info(context.AppId);
            await next();
        }

        [Message]
        public async Task OnMessage([Context] Microsoft.Teams.Api.Activities.MessageActivity activity, [Context] IContext.Client client, [Context] Microsoft.Teams.Common.Logging.ILogger log)
        {
            log.Info($"[MESSAGE] Received: {activity.Text}");
            log.Info($"[MESSAGE] From: {activity.From?.Name}");

            var text = activity.Text?.ToLowerInvariant() ?? "";

            if (text.Contains("card"))
            {
                log.Info("[CARD] Basic card requested");
                var card = CreateBasicAdaptiveCard();
                await client.Send(card);
            }
            else if (text.Contains("profile"))
            {
                log.Info("[PROFILE] Profile card requested");
                var card = CreateProfileCard();
                await client.Send(card);
            }
            else if (text.Contains("validation"))
            {
                log.Info("[VALIDATION] Validation card requested");
                var card = CreateProfileCardWithValidation();
                await client.Send(card);
            }
            else if (text.Contains("feedback"))
            {
                log.Info("[FEEDBACK] Feedback card requested");
                var card = CreateFeedbackCard();
                await client.Send(card);
            }
            else if (text.Contains("form"))
            {
                log.Info("[FORM] Task form card requested");
                var card = CreateTaskFormCard();
                await client.Send(card);
            }
            else if (text.Contains("reply"))
            {
                await client.Send("Hello! How can I assist you today?");
            }
            else
            {
                await client.Typing();
                await client.Send($"You said '{activity.Text}'. Try typing: card, profile, validation, feedback, form, or reply");
            }
        }

        [Microsoft.Teams.Apps.Activities.Invokes.AdaptiveCard.Action]
        public async Task<ActionResponse> OnCardAction([Context] AdaptiveCards.ActionActivity activity, [Context] IContext.Client client, [Context] Microsoft.Teams.Common.Logging.ILogger log)
        {
            log.Info("[CARD_ACTION] Card action received");

            var data = activity.Value?.Action?.Data;
            if (data == null || !data.ContainsKey("action"))
            {
                log.Error("[CARD_ACTION] No action specified in card data");
                return new ActionResponse.Message("No action specified") { StatusCode = 400 };
            }

            var action = data["action"]?.ToString();
            log.Info($"[CARD_ACTION] Processing action: {action}");

            switch (action)
            {
                case "submit_basic":
                    var notifyValue = data.TryGetValue("notify", out var notify) ? notify?.ToString() : "false";
                    await client.Send($"Basic card submitted! Notify setting: {notifyValue}");
                    break;

                case "submit_feedback":
                    var feedbackText = data.TryGetValue("feedback", out var feedback) ? feedback?.ToString() : "No feedback provided";
                    await client.Send($"Feedback received: {feedbackText}");
                    break;

                case "create_task":
                    var title = data.TryGetValue("title", out var t) ? t?.ToString() : "Untitled";
                    var priority = data.TryGetValue("priority", out var p) ? p?.ToString() : "medium";
                    var dueDate = data.TryGetValue("due_date", out var d) ? d?.ToString() : "No date";
                    await client.Send($"Task created!\nTitle: {title}\nPriority: {priority}\nDue: {dueDate}");
                    break;

                case "save_profile":
                    var name = data.TryGetValue("name", out var n) ? n?.ToString() : "Unknown";
                    var email = data.TryGetValue("email", out var e) ? e?.ToString() : "No email";
                    var subscribe = data.TryGetValue("subscribe", out var s) ? s?.ToString() : "false";
                    var age = data.TryGetValue("age", out var a) ? a?.ToString() : null;
                    var location = data.TryGetValue("location", out var l) ? l?.ToString() : "Not specified";

                    var response = $"Profile saved!\nName: {name}\nEmail: {email}\nSubscribed: {subscribe}";
                    if (!string.IsNullOrEmpty(age))
                        response += $"\nAge: {age}";
                    if (location != "Not specified")
                        response += $"\nLocation: {location}";

                    await client.Send(response);
                    break;

                default:
                    log.Error($"[CARD_ACTION] Unknown action: {action}");
                    return new ActionResponse.Message("Unknown action") { StatusCode = 400 };
            }

            return new ActionResponse.Message("Action processed successfully") { StatusCode = 200 };
        }

        private static AdaptiveCard CreateBasicAdaptiveCard()
        {
            return new AdaptiveCard
            {
                Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                Body = new List<CardElement>
                {
                    new TextBlock("Hello world")
                    {
                        Wrap = true,
                        Weight = TextWeight.Bolder
                    },
                    new ToggleInput("Notify me")
                    {
                        Id = "notify"
                    }
                },
                Actions = new List<Microsoft.Teams.Cards.Action>
                {
                    new ExecuteAction
                    {
                        Title = "Submit",
                        Data = new Union<string, SubmitActionData>(new SubmitActionData { NonSchemaProperties = new Dictionary<string, object?> { { "action", "submit_basic" } } }),
                        AssociatedInputs = AssociatedInputs.Auto
                    }
                }
            };
        }

        private static AdaptiveCard CreateProfileCard()
        {
            return new AdaptiveCard
            {
                Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                Body = new List<CardElement>
                {
                    new TextBlock("User Profile")
                    {
                        Weight = TextWeight.Bolder,
                        Size = TextSize.Large
                    },
                    new TextInput
                    {
                        Id = "name",
                        Label = "Name",
                        Value = "John Doe"
                    },
                    new TextInput
                    {
                        Id = "email",
                        Label = "Email",
                        Value = "john@contoso.com"
                    },
                    new ToggleInput("Subscribe to newsletter")
                    {
                        Id = "subscribe",
                        Value = "false"
                    }
                },
                Actions = new List<Microsoft.Teams.Cards.Action>
                {
                    new ExecuteAction
                    {
                        Title = "Save",
                        Data = new Union<string, SubmitActionData>(new SubmitActionData { NonSchemaProperties = new Dictionary<string, object?> { { "action", "save_profile" }, { "entity_id", "12345" } } }),
                        AssociatedInputs = AssociatedInputs.Auto
                    },
                    new OpenUrlAction("https://adaptivecards.microsoft.com")
                    {
                        Title = "Learn More"
                    }
                }
            };
        }

        private static AdaptiveCard CreateProfileCardWithValidation()
        {
            return new AdaptiveCard
            {
                Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                Body = new List<CardElement>
                {
                    new TextBlock("Profile with Validation")
                    {
                        Weight = TextWeight.Bolder,
                        Size = TextSize.Large
                    },
                    new NumberInput
                    {
                        Id = "age",
                        Label = "Age",
                        IsRequired = true,
                        Min = 0,
                        Max = 120
                    },
                    new TextInput
                    {
                        Id = "name",
                        Label = "Name",
                        IsRequired = true,
                        ErrorMessage = "Name is required"
                    },
                    new TextInput
                    {
                        Id = "location",
                        Label = "Location"
                    }
                },
                Actions = new List<Microsoft.Teams.Cards.Action>
                {
                    new ExecuteAction
                    {
                        Title = "Save",
                        Data = new Union<string, SubmitActionData>(new SubmitActionData { NonSchemaProperties = new Dictionary<string, object?> { { "action", "save_profile" } } }),
                        AssociatedInputs = AssociatedInputs.Auto
                    }
                }
            };
        }

        private static AdaptiveCard CreateFeedbackCard()
        {
            return new AdaptiveCard
            {
                Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                Body = new List<CardElement>
                {
                    new TextBlock("Feedback Form")
                    {
                        Weight = TextWeight.Bolder,
                        Size = TextSize.Large
                    },
                    new TextInput
                    {
                        Id = "feedback",
                        Label = "Your Feedback",
                        Placeholder = "Please share your thoughts...",
                        IsMultiline = true,
                        IsRequired = true
                    }
                },
                Actions = new List<Microsoft.Teams.Cards.Action>
                {
                    new ExecuteAction
                    {
                        Title = "Submit Feedback",
                        Data = new Union<string, SubmitActionData>(new SubmitActionData { NonSchemaProperties = new Dictionary<string, object?> { { "action", "submit_feedback" } } }),
                        AssociatedInputs = AssociatedInputs.Auto
                    }
                }
            };
        }

        private static AdaptiveCard CreateTaskFormCard()
        {
            return new AdaptiveCard
            {
                Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                Body = new List<CardElement>
                {
                    new TextBlock("Create New Task")
                    {
                        Weight = TextWeight.Bolder,
                        Size = TextSize.Large
                    },
                    new TextInput
                    {
                        Id = "title",
                        Label = "Task Title",
                        Placeholder = "Enter task title"
                    },
                    new TextInput
                    {
                        Id = "description",
                        Label = "Description",
                        Placeholder = "Enter task details",
                        IsMultiline = true
                    },
                    new ChoiceSetInput
                    {
                        Id = "priority",
                        Label = "Priority",
                        Value = "medium",
                        Choices = new List<Choice>
                        {
                            new() { Title = "High", Value = "high" },
                            new() { Title = "Medium", Value = "medium" },
                            new() { Title = "Low", Value = "low" }
                        }
                    },
                    new DateInput
                    {
                        Id = "due_date",
                        Label = "Due Date",
                        Value = DateTime.Now.ToString("yyyy-MM-dd")
                    }
                },
                Actions = new List<Microsoft.Teams.Cards.Action>
                {
                    new ExecuteAction
                    {
                        Title = "Create Task",
                        Data = new Union<string, SubmitActionData>(new SubmitActionData { NonSchemaProperties = new Dictionary<string, object?> { { "action", "create_task" } } }),
                        AssociatedInputs = AssociatedInputs.Auto,
                        Style = ActionStyle.Positive
                    }
                }
            };
        }

        [Microsoft.Teams.Apps.Events.Event("activity")]
        public void OnEvent(IPlugin plugin, Microsoft.Teams.Apps.Events.Event @event)
        {
            Console.WriteLine("!!HIT!!");
        }
    }
}