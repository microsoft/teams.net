using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.Tokens;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Clients;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Testing.Plugins;

using Moq;

namespace Microsoft.Teams.Apps.Tests.Activities;

public class VerifyStateActivityTests
{
    private readonly App _app = new(new()
    {
        OAuth = new()
        {
            AccountLinkingUrl = "https://my-website.com/accounts/link"
        }
    });

    private readonly IToken _token = Globals.Token;

    public VerifyStateActivityTests()
    {
        _app.AddPlugin(new TestPlugin());
    }

    [Fact]
    public async Task Should_Return_AccountLinkingUrl()
    {
        var calls = 0;
        var api = new Mock<ApiClient>("https://api.com", new CancellationToken());
        var userClient = new Mock<UserClient>();
        var userTokenClient = new Mock<UserTokenClient>();
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(),
            Expires = DateTime.UtcNow.AddHours(1), // Token expiration
            Issuer = "test",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test_test_test_test_test_test_test_test")),
                SecurityAlgorithms.HmacSha256
            )
        };

        api.SetupGet(_ => _.Users).Returns(userClient.Object);
        userClient.SetupGet(_ => _.Token).Returns(userTokenClient.Object);
        userTokenClient.Setup(_ => _.GetAsync(It.IsAny<UserTokenClient.GetTokenRequest>())).ReturnsAsync(new Api.Token.Response()
        {
            ConnectionName = "graph",
            Token = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor))
        });

        _app.Api = api.Object;
        _app.OnActivity(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(((Activity)context.Activity).ToInvoke().Name.IsSignIn);
            Assert.True(((Activity)context.Activity).ToInvoke().ToSignIn() is SignIn.VerifyStateActivity);
            context.Api = api.Object;
            return context.Next(context);
        });

        var res = await _app.Process<TestPlugin>(_token, new SignIn.VerifyStateActivity()
        {
            From = new Api.Account() { Id = "test_user_id" },
            Value = new() { State = "test_state" }
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(1, calls);
        Assert.Equal(2, res.Meta.Routes);
        Assert.Equivalent(res.Body, new { accountLinkingUrl = _app.OAuth.AccountLinkingUrl });
    }

    [Fact]
    public async Task Should_Override_AccountLinkingUrl()
    {
        var calls = 0;
        var api = new Mock<ApiClient>("https://api.com", new CancellationToken());
        var userClient = new Mock<UserClient>();
        var userTokenClient = new Mock<UserTokenClient>();
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(),
            Expires = DateTime.UtcNow.AddHours(1), // Token expiration
            Issuer = "test",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test_test_test_test_test_test_test_test")),
                SecurityAlgorithms.HmacSha256
            )
        };

        api.SetupGet(_ => _.Users).Returns(userClient.Object);
        userClient.SetupGet(_ => _.Token).Returns(userTokenClient.Object);
        userTokenClient.Setup(_ => _.GetAsync(It.IsAny<UserTokenClient.GetTokenRequest>())).Returns(Task.FromResult(new Api.Token.Response()
        {
            ConnectionName = "test",
            Token = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor))
        }));

        _app.Api = api.Object;
        _app.OnActivity(context =>
        {
            calls++;
            Assert.True(context.Activity.Type.IsInvoke);
            Assert.True(((Activity)context.Activity).ToInvoke().Name.IsSignIn);
            Assert.True(((Activity)context.Activity).ToInvoke().ToSignIn() is SignIn.VerifyStateActivity);
            return context.Next();
        });

        _app.OnVerifyState(context =>
        {
            calls++;
            return Task.FromResult<object?>(new { accountLinkingUrl = "test_linking_url" });
        });

        var res = await _app.Process<TestPlugin>(_token, new SignIn.VerifyStateActivity()
        {
            Value = new() { State = "test_state" }
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, res.Status);
        Assert.Equal(2, calls);
        Assert.Equal(2, res.Meta.Routes);
        Assert.Equivalent(res.Body, new { accountLinkingUrl = "test_linking_url" });
    }
}