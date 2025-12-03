

var builder = WebApplication.CreateBuilder(args);
// builder.AddTeams();
var app = builder.Build();
// var teamsApp = app.UseTeams();

//teamsApp.OnConversationUpdate(async context =>
//{
//    ConversationUpdateActivity cua = context.Activity;

//    string result = $"Conversation ID {cua.Conversation.Id} Members Added Count: {cua.MembersAdded.Length}, Members Removed Count {cua.MembersRemoved.Length}";

//    await context.Send(result);
//    await context.Reply("Welcome to Quote Agent!");

//});

//teamsApp.OnMessage(context =>
//{
//    return context.Reply("you said: " + context.Activity.Text);
//});

app.Run();