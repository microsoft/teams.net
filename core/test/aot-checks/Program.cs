using Microsoft.Teams.Core.Schema;

CoreActivity coreActivity = CoreActivity.FromJsonString(SampleActivities.TeamsMessage);

System.Console.WriteLine(coreActivity.ToJson());