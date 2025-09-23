using System.Text.Json;

using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.AdaptiveCards;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Apps.Plugins;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;
using System.Text.Json;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

using AdaptiveCardInvokes = Microsoft.Teams.Apps.Activities.Invokes;

namespace Samples.Cards;

public static partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // Bind the application to localhost:3978 when run with `dotnet run`
        builder.WebHost.UseUrls("http://localhost:3978");
        builder.Services.AddOpenApi();

        // Use environment variable to choose between Controller and Minimal API approaches
        var useMinimalApi = builder.Configuration.GetValue<bool>("USE_MINIMAL_API", false);
        if (useMinimalApi)
        {
            Console.WriteLine("Using Minimal API approach");
        }
        else
        {
            Console.WriteLine("Using Controller approach");
        }

        if (!useMinimalApi)
        {
            builder.Services.AddTransient<Controller>();
        }

        builder.AddTeams().AddTeamsDevTools();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        var teams = app.UseTeams();

        if (useMinimalApi)
        {
            RegisterMinimalApiHandlers(teams);
        }

        app.Run();
    }

    private static void RegisterMinimalApiHandlers(Microsoft.Teams.Apps.App teams)
    {
        // Handle messages using minimal API
        teams.OnMessage(async context =>
        {
            var activity = context.Activity;

            context.Log.Info($"[MESSAGE] Received: {SanitizeForLog(activity.Text)}");
            context.Log.Info($"[MESSAGE] From: {SanitizeForLog(activity.From?.Name ?? "unknown")}");

            var text = activity.Text?.ToLowerInvariant() ?? "";

            if (text.Contains("card"))
            {
                context.Log.Info("[CARD] Basic card requested");
                var card = CreateBasicAdaptiveCard();
                await context.Send(card);
            }
            else if (text.Contains("profile"))
            {
                context.Log.Info("[PROFILE] Profile card requested");
                var card = CreateProfileCard();
                await context.Send(card);
            }
            else if (text.Contains("validation"))
            {
                context.Log.Info("[VALIDATION] Validation card requested");
                var card = CreateProfileCardWithValidation();
                await context.Send(card);
            }
            else if (text.Contains("feedback"))
            {
                context.Log.Info("[FEEDBACK] Feedback card requested");
                var card = CreateFeedbackCard();
                await context.Send(card);
            }
            else if (text.Contains("form"))
            {
                context.Log.Info("[FORM] Task form card requested");
                var card = CreateTaskFormCard();
                await context.Send(card);
            }
            else if (text.Contains("json"))
            {
                context.Log.Info("[JSON] JSON deserialization card requested");
                var card = CreateCardFromJson();
                await context.Send(card);
            }
            else if (text.Contains("reply"))
            {
                await context.Send("Hello! How can I assist you today?");
            }
            else
            {
                await context.Typing();
                await context.Send($"You said '{activity.Text}'. Try typing: card, profile, validation, feedback, form, json, or reply");
            }
        });

        // Handle adaptive card actions using minimal API
        AdaptiveCardInvokes.AppInvokeActivityExtensions.OnAdaptiveCardAction(teams, async context =>
        {
            var activity = context.Activity;

            context.Log.Info("[CARD_ACTION] Card action received");

            var data = activity.Value?.Action?.Data;

            // Let's log the actual data structure to understand what we're working with
            context.Log.Info($"[CARD_ACTION] Raw data: {System.Text.Json.JsonSerializer.Serialize(data)}");

            if (data == null)
            {
                context.Log.Error("[CARD_ACTION] No data in card action");
                return new Microsoft.Teams.Api.AdaptiveCards.ActionResponse.Message("No data specified") { StatusCode = 400 };
            }

            // Extract action from the Value property
            string? action = data.TryGetValue("action", out var actionObj) ? actionObj?.ToString() : null;

            if (string.IsNullOrEmpty(action))
            {
                context.Log.Error("[CARD_ACTION] No action specified in card data");
                return new Microsoft.Teams.Api.AdaptiveCards.ActionResponse.Message("No action specified") { StatusCode = 400 };
            }
            context.Log.Info($"[CARD_ACTION] Processing action: {action}");

            // Helper method to extract form field values (they're at root level, not in Value)
            string? GetFormValue(string key)
            {
                if (data.TryGetValue(key, out var val))
                {
                    if (val is System.Text.Json.JsonElement element)
                        return element.GetString();
                    return val?.ToString();
                }
                return null;
            }

            switch (action)
            {
                case "submit_basic":
                    var notifyValue = GetFormValue("notify") ?? "false";
                    await context.Send($"Basic card submitted! Notify setting: {notifyValue}");
                    break;

                case "submit_feedback":
                    var feedbackText = GetFormValue("feedback") ?? "No feedback provided";
                    await context.Send($"Feedback received: {feedbackText}");
                    break;

                case "create_task":
                    var title = GetFormValue("title") ?? "Untitled";
                    var priority = GetFormValue("priority") ?? "medium";
                    var dueDate = GetFormValue("due_date") ?? "No date";
                    await context.Send($"Task created!\nTitle: {title}\nPriority: {priority}\nDue: {dueDate}");
                    break;

                case "save_profile":
                    var name = GetFormValue("name") ?? "Unknown";
                    var email = GetFormValue("email") ?? "No email";
                    var subscribe = GetFormValue("subscribe") ?? "false";
                    var age = GetFormValue("age");
                    var location = GetFormValue("location") ?? "Not specified";

                    var response = $"Profile saved!\nName: {name}\nEmail: {email}\nSubscribed: {subscribe}";
                    if (!string.IsNullOrEmpty(age))
                        response += $"\nAge: {age}";
                    if (location != "Not specified")
                        response += $"\nLocation: {location}";

                    await context.Send(response);
                    break;

                case "test_json":
                    await context.Send("✅ JSON deserialization test successful! The card was properly created from JSON and the action was processed correctly.");
                    break;

                default:
                    context.Log.Error($"[CARD_ACTION] Unknown action: {action}");
                    return new Microsoft.Teams.Api.AdaptiveCards.ActionResponse.Message("Unknown action") { StatusCode = 400 };
            }

            return new Microsoft.Teams.Api.AdaptiveCards.ActionResponse.Message("Action processed successfully") { StatusCode = 200 };
        });
    }

    // Helper method to sanitize user input for logging
    private static string SanitizeForLog(string input)
    {
        if (input == null) return "";
        // Remove carriage returns and line feeds to prevent log forging
        return input.Replace("\r", "").Replace("\n", "");
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

    private static AdaptiveCard CreateCardFromJson()
    {
        // JSON similar to the Python create_model_validate_card example
        var cardJson = @"{
            ""type"": ""AdaptiveCard"",
            ""body"": [
                {
                    ""type"": ""ColumnSet"",
                    ""columns"": [
                        {
                            ""type"": ""Column"",
                            ""verticalContentAlignment"": ""center"",
                            ""items"": [
                                {
                                    ""type"": ""Image"",
                                    ""style"": ""Person"",
                                    ""url"": ""https://aka.ms/AAp9xo4"",
                                    ""size"": ""Small"",
                                    ""altText"": ""Portrait of David Claux""
                                }
                            ],
                            ""width"": ""auto""
                        },
                        {
                            ""type"": ""Column"",
                            ""spacing"": ""medium"",
                            ""verticalContentAlignment"": ""center"",
                            ""items"": [
                                {
                                    ""type"": ""TextBlock"",
                                    ""weight"": ""Bolder"",
                                    ""text"": ""David Claux"",
                                    ""wrap"": true
                                }
                            ],
                            ""width"": ""auto""
                        },
                        {
                            ""type"": ""Column"",
                            ""spacing"": ""medium"",
                            ""verticalContentAlignment"": ""center"",
                            ""items"": [
                                {
                                    ""type"": ""TextBlock"",
                                    ""text"": ""Principal Platform Architect at Microsoft"",
                                    ""isSubtle"": true,
                                    ""wrap"": true
                                }
                            ],
                            ""width"": ""stretch""
                        }
                    ]
                },
                {
                    ""type"": ""TextBlock"",
                    ""text"": ""This card was created from JSON deserialization!"",
                    ""wrap"": true,
                    ""color"": ""good"",
                    ""spacing"": ""medium""
                }
            ],
            ""actions"": [
                {
                    ""type"": ""Action.Execute"",
                    ""title"": ""Test JSON Action"",
                    ""data"": {
                        ""Value"": {
                            ""action"": ""test_json""
                        }
                    },
                    ""associatedInputs"": ""auto""
                }
            ],
            ""version"": ""1.5"",
            ""schema"": ""http://adaptivecards.io/schemas/adaptive-card.json""
        }";

        try
        {
            var card = JsonSerializer.Deserialize<AdaptiveCard>(cardJson);

            return card ?? throw new InvalidOperationException("Failed to deserialize card");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deserializing card JSON: {ex.Message}");
            // If deserialization fails, return a fallback card with error info
            return new AdaptiveCard
            {
                Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                Body = new List<CardElement>
                {
                    new TextBlock("JSON Deserialization Test")
                    {
                        Weight = TextWeight.Bolder,
                        Size = TextSize.Large,
                        Color = TextColor.Attention
                    },
                    new TextBlock($"Deserialization failed: {ex.Message}")
                    {
                        Wrap = true,
                        Color = TextColor.Attention
                    }
                }
            };
        }
    }

    [TeamsController]
    public class Controller
    {
        [Message]
        public async Task OnMessage([Context] Microsoft.Teams.Api.Activities.MessageActivity activity, [Context] IContext.Client client, [Context] Microsoft.Teams.Common.Logging.ILogger log)
        {
            log.Info($"[MESSAGE] Received: {SanitizeForLog(activity.Text)}");
            log.Info($"[MESSAGE] From: {SanitizeForLog(activity.From?.Name ?? "unknown")}");

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
            else if (text.Contains("json"))
            {
                log.Info("[JSON] JSON deserialization card requested");
                var card = CreateCardFromJson();
                await client.Send(card);
            }
            else if (text.Contains("reply"))
            {
                await client.Send("Hello! How can I assist you today?");
            }
            else
            {
                await client.Typing();
                await client.Send($"You said '{activity.Text}'. Try typing: card, profile, validation, feedback, form, json, or reply");
            }
        }

        [Microsoft.Teams.Apps.Activities.Invokes.AdaptiveCard.Action]
        public async Task<ActionResponse> OnCardAction([Context] AdaptiveCards.ActionActivity activity, [Context] IContext.Client client, [Context] Microsoft.Teams.Common.Logging.ILogger log)
        {
            log.Info("[CARD_ACTION] Card action received");

            var data = activity.Value?.Action?.Data;

            // Let's log the actual data structure to understand what we're working with
            log.Info($"[CARD_ACTION] Raw data: {System.Text.Json.JsonSerializer.Serialize(data)}");

            if (data == null)
            {
                log.Error("[CARD_ACTION] No data in card action");
                return new ActionResponse.Message("No data specified") { StatusCode = 400 };
            }

            // Extract action from the Value property
            string? action = data.TryGetValue("action", out var actionObj) ? actionObj?.ToString() : null;

            if (string.IsNullOrEmpty(action))
            {
                log.Error("[CARD_ACTION] No action specified in card data");
                return new ActionResponse.Message("No action specified") { StatusCode = 400 };
            }
            log.Info($"[CARD_ACTION] Processing action: {action}");

            // Helper method to extract form field values (they're at root level, not in Value)
            string? GetFormValue(string key)
            {
                if (data.TryGetValue(key, out var val))
                {
                    if (val is System.Text.Json.JsonElement element)
                        return element.GetString();
                    return val?.ToString();
                }
                return null;
            }

            switch (action)
            {
                case "submit_basic":
                    var notifyValue = GetFormValue("notify") ?? "false";
                    await client.Send($"Basic card submitted! Notify setting: {notifyValue}");
                    break;

                case "submit_feedback":
                    var feedbackText = GetFormValue("feedback") ?? "No feedback provided";
                    await client.Send($"Feedback received: {feedbackText}");
                    break;

                case "create_task":
                    var title = GetFormValue("title") ?? "Untitled";
                    var priority = GetFormValue("priority") ?? "medium";
                    var dueDate = GetFormValue("due_date") ?? "No date";
                    await client.Send($"Task created!\nTitle: {title}\nPriority: {priority}\nDue: {dueDate}");
                    break;

                case "save_profile":
                    var name = GetFormValue("name") ?? "Unknown";
                    var email = GetFormValue("email") ?? "No email";
                    var subscribe = GetFormValue("subscribe") ?? "false";
                    var age = GetFormValue("age");
                    var location = GetFormValue("location") ?? "Not specified";

                    var response = $"Profile saved!\nName: {name}\nEmail: {email}\nSubscribed: {subscribe}";
                    if (!string.IsNullOrEmpty(age))
                        response += $"\nAge: {age}";
                    if (location != "Not specified")
                        response += $"\nLocation: {location}";

                    await client.Send(response);
                    break;

                case "test_json":
                    await client.Send("✅ JSON deserialization test successful! The card was properly created from JSON and the action was processed correctly.");
                    break;

                default:
                    log.Error($"[CARD_ACTION] Unknown action: {action}");
                    return new ActionResponse.Message("Unknown action") { StatusCode = 400 };
            }

            return new ActionResponse.Message("Action processed successfully") { StatusCode = 200 };
        }

        [Microsoft.Teams.Apps.Events.Event("activity")]
        public void OnEvent(IPlugin plugin, Microsoft.Teams.Apps.Events.Event @event)
        {
            Console.WriteLine("!!HIT!!");
        }
    }
}