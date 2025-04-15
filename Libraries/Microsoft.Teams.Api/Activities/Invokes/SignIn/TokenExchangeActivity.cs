using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public partial class SignIn : StringEnum
    {
        public static readonly SignIn TokenExchange = new("signin/tokenExchange");
        public bool IsTokenExchange => TokenExchange.Equals(Value);
    }
}

public static partial class SignIn
{
    public class TokenExchangeActivity() : SignInActivity(Name.SignIn.TokenExchange)
    {
        /// <summary>
        /// A value that is associated with the activity.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(32)]
        public new required Api.SignIn.ExchangeToken Value { get; set; }
    }
}