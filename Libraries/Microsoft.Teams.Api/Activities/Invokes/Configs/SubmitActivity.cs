using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public partial class Configs
    {
        public static readonly Configs Submit = new("config/submit");
        public bool IsSubmit => Submit.Equals(Value);
    }
}

public static partial class Configs
{
    public class SubmitActivity : ConfigActivity
    {
        public SubmitActivity(object? value = null) : base(Name.Configs.Submit)
        {
            Value = value;
        }
    }
}