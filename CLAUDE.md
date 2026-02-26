# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Startup

Before responding to the user's first message, complete these steps:

### 1. Read knowledge files
- Read `Claude-KB.md` in this directory (domain knowledge, lessons learned). Create it if it doesn't exist with a `## Lessons Learned` heading.
- **Don't read md files from the parent directory unless the user requests it** — this slows down session start.
- Look for a `*-private.md` file matching the user's name (e.g., `Rajan-private.md`). If one exists, read it — it contains personal TODOs, preferences, and reminders. These files are gitignored and never committed.
- The private file may reference a durable location (e.g., a private git repo). If it does, also read and update that location for persistent notes and TODOs.

### 2. Read session context
- Read `session-context.md` if it exists. It contains ephemeral state from the previous session: what was in flight, what to pick up, any "don't forget" items. This file is gitignored and overwritten each save.
- Surface relevant items in the greeting (e.g., "Last session you were working on PR 1234").

### 3. Greet the user and surface
- Any open TODOs or reminders from private notes
- Common scenarios / quick-start commands:
  - **Build the solution** — `dotnet build`
  - **Run all tests** — `dotnet test`
  - **Run a specific test** — `dotnet test --filter "FullyQualifiedName~TestName"`
  - **Run a sample app** — `dotnet run --project Samples/Samples.Echo`
  - **Format code** — `dotnet format`
  - **Create NuGet packages** — `dotnet pack`

## Build Commands

```bash
dotnet build              # Build solution
dotnet test               # Run all tests
dotnet test -v d          # Run tests with detailed verbosity
dotnet format             # Format code (EditorConfig enforced)
dotnet pack               # Create NuGet packages
```

Run a specific test project:
```bash
dotnet test Tests/Microsoft.Teams.Apps.Tests
```

Run a single test by name:
```bash
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

Run tests with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

Run a specific sample:
```bash
dotnet run --project Samples/Samples.Echo
dotnet run --project Samples/Samples.Lights
```

## Development Workflow

- Cannot push directly to main - all changes require a pull request
- Create a feature branch, make changes, then open a PR
- CI runs build, test, and lint checks on PRs

## Architecture Overview

This is the Microsoft Teams SDK for .NET (`Microsoft.Teams.sln`) - a suite of packages for building Teams bots and apps.

### Core Libraries (Libraries/)

- **Microsoft.Teams.Apps** - Core bot functionality: activity handling, message processing, routing, context management
- **Microsoft.Teams.AI** - AI/LLM integration: chat plugins, function definitions, prompt templates
- **Microsoft.Teams.AI.Models.OpenAI** - OpenAI-specific model implementation
- **Microsoft.Teams.Api** - Teams API client for bot-to-Teams communication
- **Microsoft.Teams.Cards** - Adaptive Cards support
- **Microsoft.Teams.Common** - Shared utilities, JSON helpers, HTTP, logging, storage patterns

### Extensions (Libraries/Microsoft.Teams.Extensions/)

- **Configuration** - Configuration helpers
- **Hosting** - ASP.NET Core DI integration
- **Logging** - Microsoft.Extensions.Logging integration
- **Graph** - Microsoft Graph integration

### Plugins (Libraries/Microsoft.Teams.Plugins/)

- **AspNetCore** - Core middleware for ASP.NET Core
- **AspNetCore.DevTools** - Development tools
- **AspNetCore.BotBuilder** - Bot Builder SDK adapter
- **External.Mcp** / **External.McpClient** - Model Context Protocol integration

## Code Patterns

### Basic App Setup

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddTeams();                    // Register Teams services
var app = builder.Build();
var teams = app.UseTeams();            // Get Teams middleware

teams.OnMessage(async context => {     // Handle messages
    await context.Send("Hello!");
});

app.Run();
```

### AI Plugin

```csharp
[Prompt]
[Prompt.Description("description")]
[Prompt.Instructions("system instructions")]
public class MyPrompt(IContext.Accessor accessor)
{
    [Function]
    [Function.Description("what this function does")]
    public string MyFunction([Param("param description")] string input)
    {
        return "result";
    }
}
```

## Code Style

EditorConfig is strictly enforced. Key conventions:

- **Namespaces**: File-scoped (`namespace Foo;`)
- **Fields**: `_camelCase` for private, `s_camelCase` for private static
- **Nullable**: Enabled throughout
- **Async**: All async methods, CancellationToken support

All files require Microsoft copyright header:
```csharp
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
```

## Testing

- xUnit with Moq for mocking, implicit `using Xunit` in test projects
- Test projects target net9.0 (libraries target net8.0)
- Test naming: `{LibraryName}.Tests` in `Tests/` directory
- Use `Microsoft.Teams.Apps.Testing` for test utilities

## Lessons Learned

This workspace is a **learning system**. Claude-KB.md contains a `## Lessons Learned` section that persists knowledge across sessions.

### When to add an entry

Proactively add a lesson whenever you encounter:

- **Unexpected behavior** — an API, tool, or workflow didn't work as expected and you found the cause
- **Workarounds** — a problem required a non-obvious solution that future sessions should know about
- **User preferences** — the user corrects your approach or states a preference
- **Process discoveries** — you learn how something actually works vs. how it's documented
- **Pitfalls** — something that wasted time and could be avoided next time

### How to add an entry

Append to the `## Lessons Learned` section in `Claude-KB.md` using this format:

```markdown
### YYYY-MM-DD: Short descriptive title
Description of what happened and what to do differently. Keep it concise and actionable.
```

### Guidelines

- Write for your future self — assume no prior context from this session
- Be specific: include tool names, flag names, error messages, or exact steps
- Don't duplicate existing entries — read the section first
- One entry per distinct lesson; don't bundle unrelated things
- Ask the user before adding if you're unsure whether something qualifies
