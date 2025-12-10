# Microsoft.Bot.Core



## Testing

### Install Playground

Linux
```
curl -s https://raw.githubusercontent.com/OfficeDev/microsoft-365-agents-toolkit/dev/.github/scripts/install-agentsplayground-linux.sh | bash
```

Windows
```
winget install m365agentsplayground
```


### Run Scenarios

```
dotnet samples/scenarios/middleware.cs -- --urls "http://localhost:3978"
```
