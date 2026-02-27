using Microsoft.Teams.Bot.Core.Schema;

CoreActivity coreActivity = CoreActivity.FromJsonString(SampleActivities.TeamsMessage);

System.Console.WriteLine(coreActivity.ToJson());