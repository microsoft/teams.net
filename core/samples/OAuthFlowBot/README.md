# OAuthFlowBot

This sample shows multiple OAuth flows in one bot. It demonstrates how to keep separate user sign-in experiences for different services and how to reuse the resulting tokens in bot commands.

## Prerequisites

- Bot registered and installed in Teams.
- Azure Bot OAuth connection settings created for both Graph and GitHub.
- [optional for multiple instances] Redis available and configured via `ConnectionStrings__Redis` (for shared OAuth pending state across instances).

## OAuth connection setup

Configure these two connection settings in your bot resource:

| Connection name | Provider | Scopes |
|---|---|---|
| `sso` | Azure AD v2 | `User.Read Calendars.Read` |
| `gh` | GitHub | `repo read:user` |

## What it shows

- Microsoft Graph OAuth flow and GitHub OAuth flow side by side.
- Sign-in commands that can trigger one or both flows.
- Status and sign-out commands for checking and clearing connection state.
- Token-backed calls to Graph and GitHub after login.

It is a good reference for combining multiple user-delegated services in one bot while keeping the sign-in flows separate and explicit.

## Commands

| Command | Behavior |
|---|---|
| `help` | Shows all OAuth and token-test commands |
| `login` | Starts sign-in for both Graph and GitHub |
| `login graph` | Starts sign-in for Graph only |
| `login github` | Starts sign-in for GitHub only |
| `status` | Lists all connection states (`connected` / `not connected`) |
| `my ad user` | Calls `https://graph.microsoft.com/v1.0/me` with Graph token |
| `my gh user` | Calls `https://api.github.com/user` with GitHub token |
| `logout` | Signs out of both connections |
| `logout graph` | Signs out of Graph only |
| `logout github` | Signs out of GitHub only |

## OAuth behavior notes

- `SignInAsync(...)` returns `null` when a sign-in card is sent and auth is in progress.
- `SignInAsync(...)` returns a token immediately when already signed in.
- The sample wires both success and failure callbacks for each connection via `OnSignInComplete` and `OnSignInFailure`.

## Running the Sample

~~~bash
dotnet run --project samples/OAuthFlowBot/OAuthFlowBot.csproj
~~~

In Teams, send `help`, then run `login`, `status`, `my ad user`, `my gh user`, and `logout` to validate the full flow.
