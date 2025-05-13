using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public partial class Configs
    {
        public static readonly Configs Fetch = new("config/fetch");
        public bool IsFetch => Fetch.Equals(Value);
    }
}

public static partial class Configs
{
    public class FetchActivity : ConfigActivity
    {
        public FetchActivity(object? value = null) : base(Name.Configs.Fetch)
        {
            Value = value;
        }
    }
}