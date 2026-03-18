using System.Text.Json;

using Microsoft.Teams.Api.AdaptiveCards;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Extensions;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:3978");
builder.Services.AddOpenApi();
builder.AddTeams().AddTeamsDevTools();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
var teams = app.UseTeams();

teams.OnMessage(async (context, cancellationToken) =>
{
    var activity = context.Activity;
    context.Log.Info($"[MESSAGE] Received: {SanitizeForLog(activity.Text)}");
    context.Log.Info($"[MESSAGE] From: {SanitizeForLog(activity.From?.Name ?? "unknown")}");

    var text = activity.Text?.ToLowerInvariant() ?? "";

    if (text.Contains("card"))
    {
        context.Log.Info("[CARD] Basic card requested");
        var card = CreateBasicAdaptiveCard();
        await context.Send(card, cancellationToken);
    }
    else if (text.Contains("profile"))
    {
        context.Log.Info("[PROFILE] Profile card requested");
        var card = CreateProfileCard();
        await context.Send(card, cancellationToken);
    }
    else if (text.Contains("validation"))
    {
        context.Log.Info("[VALIDATION] Validation card requested");
        var card = CreateProfileCardWithValidation();
        await context.Send(card, cancellationToken);
    }
    else if (text.Contains("feedback"))
    {
        context.Log.Info("[FEEDBACK] Feedback card requested");
        var card = CreateFeedbackCard();
        await context.Send(card, cancellationToken);
    }
    else if (text.Contains("form"))
    {
        context.Log.Info("[FORM] Task form card requested");
        var card = CreateTaskFormCard();
        await context.Send(card, cancellationToken);
    }
    else if (text.Contains("json"))
    {
        context.Log.Info("[JSON] JSON deserialization card requested");
        var card = CreateCardFromJson();
        await context.Send(card, cancellationToken);
    }
    else if (text.Contains("reply"))
    {
        await context.Send("Hello! How can I assist you today?", cancellationToken);
    }
    else
    {
        await context.Typing(cancellationToken: cancellationToken);
        await context.Send($"You said '{activity.Text}'. Try typing: card, profile, validation, feedback, form, json, or reply", cancellationToken);
    }
});

teams.OnAdaptiveCardAction(async (context, cancellationToken) =>
{
    var activity = context.Activity;
    context.Log.Info("[CARD_ACTION] Card action received");

    var data = activity.Value?.Action?.Data;

    context.Log.Info($"[CARD_ACTION] Raw data: {JsonSerializer.Serialize(data)}");

    if (data == null)
    {
        context.Log.Error("[CARD_ACTION] No data in card action");
        return new ActionResponse.Message("No data specified") { StatusCode = 400 };
    }

    string? action = data.TryGetValue("action", out var actionObj) ? actionObj?.ToString() : null;

    if (string.IsNullOrEmpty(action))
    {
        context.Log.Error("[CARD_ACTION] No action specified in card data");
        return new ActionResponse.Message("No action specified") { StatusCode = 400 };
    }
    context.Log.Info($"[CARD_ACTION] Processing action: {action}");

    string? GetFormValue(string key)
    {
        if (data.TryGetValue(key, out var val))
        {
            if (val is JsonElement element)
                return element.GetString();
            return val?.ToString();
        }
        return null;
    }

    switch (action)
    {
        case "submit_basic":
            var notifyValue = GetFormValue("notify") ?? "false";
            await context.Send($"Basic card submitted! Notify setting: {notifyValue}", cancellationToken);
            break;

        case "submit_feedback":
            var feedbackText = GetFormValue("feedback") ?? "No feedback provided";
            await context.Send($"Feedback received: {feedbackText}", cancellationToken);
            break;

        case "create_task":
            var title = GetFormValue("title") ?? "Untitled";
            var priority = GetFormValue("priority") ?? "medium";
            var dueDate = GetFormValue("due_date") ?? "No date";
            await context.Send($"Task created!\nTitle: {title}\nPriority: {priority}\nDue: {dueDate}", cancellationToken);
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

            await context.Send(response, cancellationToken);
            break;

        case "test_json":
            await context.Send("JSON deserialization test successful!", cancellationToken);
            break;

        default:
            context.Log.Error($"[CARD_ACTION] Unknown action: {action}");
            return new ActionResponse.Message("Unknown action") { StatusCode = 400 };
    }

    return new ActionResponse.Message("Action processed successfully") { StatusCode = 200 };
});

app.Run();

static string SanitizeForLog(string? input)
{
    if (input == null) return "";
    return input.Replace("\r", "").Replace("\n", "");
}

static Microsoft.Teams.Cards.AdaptiveCard CreateBasicAdaptiveCard()
{
    return new Microsoft.Teams.Cards.AdaptiveCard
    {
        Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
        Body = new List<CardElement>
        {
            new TextBlock("Hello world") { Wrap = true, Weight = TextWeight.Bolder },
            new ToggleInput("Notify me") { Id = "notify" }
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

static Microsoft.Teams.Cards.AdaptiveCard CreateProfileCard()
{
    return new Microsoft.Teams.Cards.AdaptiveCard
    {
        Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
        Body = new List<CardElement>
        {
            new TextBlock("User Profile") { Weight = TextWeight.Bolder, Size = TextSize.Large },
            new TextInput { Id = "name", Label = "Name", Value = "John Doe" },
            new TextInput { Id = "email", Label = "Email", Value = "john@contoso.com" },
            new ToggleInput("Subscribe to newsletter") { Id = "subscribe", Value = "false" }
        },
        Actions = new List<Microsoft.Teams.Cards.Action>
        {
            new ExecuteAction
            {
                Title = "Save",
                Data = new Union<string, SubmitActionData>(new SubmitActionData { NonSchemaProperties = new Dictionary<string, object?> { { "action", "save_profile" }, { "entity_id", "12345" } } }),
                AssociatedInputs = AssociatedInputs.Auto
            },
            new OpenUrlAction("https://adaptivecards.microsoft.com") { Title = "Learn More" }
        }
    };
}

static Microsoft.Teams.Cards.AdaptiveCard CreateProfileCardWithValidation()
{
    return new Microsoft.Teams.Cards.AdaptiveCard
    {
        Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
        Body = new List<CardElement>
        {
            new TextBlock("Profile with Validation") { Weight = TextWeight.Bolder, Size = TextSize.Large },
            new NumberInput { Id = "age", Label = "Age", IsRequired = true, Min = 0, Max = 120 },
            new TextInput { Id = "name", Label = "Name", IsRequired = true, ErrorMessage = "Name is required" },
            new TextInput { Id = "location", Label = "Location" }
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

static Microsoft.Teams.Cards.AdaptiveCard CreateFeedbackCard()
{
    return new Microsoft.Teams.Cards.AdaptiveCard
    {
        Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
        Body = new List<CardElement>
        {
            new TextBlock("Feedback Form") { Weight = TextWeight.Bolder, Size = TextSize.Large },
            new TextInput { Id = "feedback", Label = "Your Feedback", Placeholder = "Please share your thoughts...", IsMultiline = true, IsRequired = true }
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

static Microsoft.Teams.Cards.AdaptiveCard CreateTaskFormCard()
{
    return new Microsoft.Teams.Cards.AdaptiveCard
    {
        Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
        Body = new List<CardElement>
        {
            new TextBlock("Create New Task") { Weight = TextWeight.Bolder, Size = TextSize.Large },
            new TextInput { Id = "title", Label = "Task Title", Placeholder = "Enter task title" },
            new TextInput { Id = "description", Label = "Description", Placeholder = "Enter task details", IsMultiline = true },
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
            new DateInput { Id = "due_date", Label = "Due Date", Value = DateTime.Now.ToString("yyyy-MM-dd") }
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

static Microsoft.Teams.Cards.AdaptiveCard CreateCardFromJson()
{
    var cardJson = @"{
        ""type"": ""AdaptiveCard"",
        ""body"": [
            {
                ""type"": ""ColumnSet"",
                ""columns"": [
                    {
                        ""type"": ""Column"",
                        ""verticalContentAlignment"": ""center"",
                        ""items"": [{ ""type"": ""Image"", ""style"": ""Person"", ""url"": ""https://aka.ms/AAp9xo4"", ""size"": ""Small"", ""altText"": ""Portrait of David Claux"" }],
                        ""width"": ""auto""
                    },
                    {
                        ""type"": ""Column"",
                        ""spacing"": ""medium"",
                        ""verticalContentAlignment"": ""center"",
                        ""items"": [{ ""type"": ""TextBlock"", ""weight"": ""Bolder"", ""text"": ""David Claux"", ""wrap"": true }],
                        ""width"": ""auto""
                    },
                    {
                        ""type"": ""Column"",
                        ""spacing"": ""medium"",
                        ""verticalContentAlignment"": ""center"",
                        ""items"": [{ ""type"": ""TextBlock"", ""text"": ""Principal Platform Architect at Microsoft"", ""isSubtle"": true, ""wrap"": true }],
                        ""width"": ""stretch""
                    }
                ]
            },
            { ""type"": ""TextBlock"", ""text"": ""This card was created from JSON deserialization!"", ""wrap"": true, ""color"": ""good"", ""spacing"": ""medium"" }
        ],
        ""actions"": [{ ""type"": ""Action.Execute"", ""title"": ""Test JSON Action"", ""data"": { ""Value"": { ""action"": ""test_json"" } }, ""associatedInputs"": ""auto"" }],
        ""version"": ""1.5"",
        ""schema"": ""http://adaptivecards.io/schemas/adaptive-card.json""
    }";

    try
    {
        var card = JsonSerializer.Deserialize<Microsoft.Teams.Cards.AdaptiveCard>(cardJson);
        return card ?? throw new InvalidOperationException("Failed to deserialize card");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error deserializing card JSON: {ex.Message}");
        return new Microsoft.Teams.Cards.AdaptiveCard
        {
            Schema = "http://adaptivecards.io/schemas/adaptive-card.json",
            Body = new List<CardElement>
            {
                new TextBlock("JSON Deserialization Test") { Weight = TextWeight.Bolder, Size = TextSize.Large, Color = TextColor.Attention },
                new TextBlock($"Deserialization failed: {ex.Message}") { Wrap = true, Color = TextColor.Attention }
            }
        };
    }
}