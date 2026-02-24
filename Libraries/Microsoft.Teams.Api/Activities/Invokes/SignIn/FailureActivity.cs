// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api.Activities.Invokes;

public partial class Name : StringEnum
{
    public partial class SignIn : StringEnum
    {
        public static readonly SignIn Failure = new("signin/failure");
        public bool IsFailure => Failure.Equals(Value);
    }
}

public static partial class SignIn
{
    /// <summary>
    /// Represents a signin/failure invoke activity sent by Teams when SSO token exchange fails.
    /// </summary>
    public class FailureActivity() : SignInActivity(Name.SignIn.Failure)
    {
        /// <summary>
        /// A value that is associated with the activity.
        /// </summary>
        [JsonPropertyName("value")]
        [JsonPropertyOrder(32)]
        public new required Api.SignIn.Failure Value { get; set; }
    }
}
