using System.Text.Json.Serialization;

using Microsoft.Teams.Common;

namespace Microsoft.Teams.Api;

[JsonConverter(typeof(JsonConverter<ContentType>))]
public partial class ContentType(string value) : StringEnum(value)
{
    public static readonly ContentType Html = new("html");
    public bool IsHtml => Html.Equals(Value);

    public static readonly ContentType Text = new("text");
    public bool IsText => Text.Equals(Value);

    public static readonly ContentType AdaptiveCard = new("application/vnd.microsoft.card.adaptive");
    public bool IsAdaptiveCard => AdaptiveCard.Equals(Value);

    public static readonly ContentType AnimationCard = new("application/vnd.microsoft.card.animation");
    public bool IsAnimationCard => AnimationCard.Equals(Value);

    public static readonly ContentType AudioCard = new("application/vnd.microsoft.card.audio");
    public bool IsAudioCard => AudioCard.Equals(Value);

    public static readonly ContentType HeroCard = new("application/vnd.microsoft.card.hero");
    public bool IsHeroCard => HeroCard.Equals(Value);

    public static readonly ContentType OAuthCard = new("application/vnd.microsoft.card.oauth");
    public bool IsOAuthCard => OAuthCard.Equals(Value);

    public static readonly ContentType SignInCard = new("application/vnd.microsoft.card.signin");
    public bool IsSignInCard => SignInCard.Equals(Value);

    public static readonly ContentType ThumbnailCard = new("application/vnd.microsoft.card.thumbnail");
    public bool IsThumbnailCard => ThumbnailCard.Equals(Value);

    public static readonly ContentType VideoCard = new("application/vnd.microsoft.card.video");
    public bool IsVideoCard => VideoCard.Equals(Value);

    public static readonly ContentType Message = new("application/vnd.microsoft.activity.message");
    public bool IsMessage => Message.Equals(Value);

    public static readonly ContentType Error = new("application/vnd.microsoft.error");
    public bool IsError => Error.Equals(Value);

    public static readonly ContentType LoginRequest = new("application/vnd.microsoft.activity.loginRequest");
    public bool IsLoginRequest => LoginRequest.Equals(Value);

    public static readonly ContentType IncorrectAuthCode = new("application/vnd.microsoft.error.incorrectAuthCode");
    public bool IsIncorrectAuthCode => IncorrectAuthCode.Equals(Value);

    public static readonly ContentType PreConditionFailed = new("application/vnd.microsoft.error.preconditionFailed");
    public bool IsPreConditionFailed => PreConditionFailed.Equals(Value);
}