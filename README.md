# ![Teams SDK Icon](./Assets/icon.png)

# Teams SDK: DotNet

[![Version](https://img.shields.io/github/v/release/microsoft/teams.net?label=version)](#)

a suite of packages used to build on the Teams Platform.

[![📖 Getting Started](https://img.shields.io/badge/📖%20Getting%20Started-blue?style=for-the-badge)](https://microsoft.github.io/teams-sdk)

## Questions & Issues

- **Questions or Feature Requests**: Please use [GitHub Discussions](https://github.com/microsoft/teams-sdk/discussions)
- **Bug Reports**: Please [open an issue](https://github.com/microsoft/teams.net/issues/new/choose)

### Build

```bash
$: dotnet build
```

### Clean

```bash
$: dotnet clean
```

### Format

```bash
$: dotnet format
```

### Test

```bash
$: dotnet test
```

## Public Preview Packages

> ⚠️ **Preview Packages**: In addition to stable releases, we publish preview packages (versioned with `-preview` suffix) to [nuget.org](https://www.nuget.org/profiles/teams-sdk) for early access to new features.
>
> **About preview packages:**
> - Preview builds may contain bugs, incomplete features, or breaking changes between versions
> - APIs in preview packages are subject to change without notice
> - You are welcome to [file issues](https://github.com/microsoft/teams.net/issues) for bugs or feedback, but they may not be addressed with the same priority as stable releases
> - **Preview packages are not recommended for production workloads**

## Packages

> ℹ️ core packages used to build client/server apps for Teams.

- [`Microsoft.Teams.Apps`](./Libraries/Microsoft.Teams.Apps/README.md)
- [`Microsoft.Teams.Apps.Testing`](./Libraries/Microsoft.Teams.Apps.Testing/README.md)
- [`Microsoft.Teams.AI`](./Libraries/Microsoft.Teams.AI/README.md)
- [`Microsoft.Teams.AI.Models.OpenAI`](./Libraries/Microsoft.Teams.AI.Models.OpenAI/README.md)
- [`Microsoft.Teams.Api`](./Libraries/Microsoft.Teams.Api/README.md)
- [`Microsoft.Teams.Cards`](./Libraries/Microsoft.Teams.Cards/README.md)
- [`Microsoft.Teams.Common`](./Libraries/Microsoft.Teams.Common/README.md)
- [`Microsoft.Teams.Extensions.Configuration`](./Libraries/Microsoft.Teams.Extensions/Microsoft.Teams.Extensions.Configuration/README.md)
- [`Microsoft.Teams.Extensions.Hosting`](./Libraries/Microsoft.Teams.Extensions/Microsoft.Teams.Extensions.Hosting/README.md)
- [`Microsoft.Teams.Extensions.Logging`](./Libraries/Microsoft.Teams.Extensions/Microsoft.Teams.Extensions.Logging/README.md)
- [`Microsoft.Teams.Extensions.Graph`](./Libraries/Microsoft.Teams.Extensions/Microsoft.Teams.Extensions.Graph/README.md)
- [`Microsoft.Teams.Plugins.AspNetCore`](./Libraries/Microsoft.Teams.Plugins/Microsoft.Teams.Plugins.AspNetCore/README.md)
- [`Microsoft.Teams.Plugins.AspNetCore.DevTools`](./Libraries/Microsoft.Teams.Plugins/Microsoft.Teams.Plugins.AspNetCore.DevTools/README.md)
- [`Microsoft.Teams.Plugins.AspNetCore.BotBuilder`](./Libraries/Microsoft.Teams.Plugins/Microsoft.Teams.Plugins.AspNetCore.BotBuilder/README.md)

## External Packages

> ℹ️ external packages (typically plugins) used to integrate with other platforms.

- [`Microsoft.Teams.Plugins.External.Mcp`](./Libraries/Microsoft.Teams.Plugins/Microsoft.Teams.Plugins.External/Microsoft.Teams.Plugins.External.Mcp/README.md)
- [`Microsoft.Teams.Plugins.External.McpClient`](./Libraries/Microsoft.Teams.Plugins/Microsoft.Teams.Plugins.External/Microsoft.Teams.Plugins.External.McpClient/README.md)

