# GitHub Copilot Instructions for Teams.NET SDK

This repository contains the Teams SDK for .NET, a suite of packages for building applications on the Teams Platform.

## Build and Development Commands

### Build
```bash
dotnet build
```

### Clean
```bash
dotnet clean
```

### Format Code
```bash
dotnet format
```

### Test
```bash
dotnet test
```

For verbose test output:
```bash
dotnet test -v d
```

### Restore Dependencies
```bash
dotnet restore
```

## Project Structure

- **Libraries/** - Core SDK packages for Teams applications
  - `Microsoft.Teams.Apps` - Core application framework
  - `Microsoft.Teams.AI` - AI/ML capabilities for Teams
  - `Microsoft.Teams.Api` - Teams API client
  - `Microsoft.Teams.Cards` - Adaptive Cards support
  - `Microsoft.Teams.Common` - Shared utilities and types
  - `Microsoft.Teams.Extensions.*` - Extension packages for configuration, hosting, logging, and Graph
  - `Microsoft.Teams.Plugins.*` - Plugin packages for ASP.NET Core, BotBuilder, and external integrations
- **Samples/** - Example applications demonstrating SDK usage
- **Tests/** - Unit and integration tests for all packages

## C# Coding Conventions

### General Style
- Use **4 spaces** for indentation in C# files (not tabs)
- Follow .NET naming conventions:
  - PascalCase for types, methods, properties, public fields
  - camelCase for local variables, parameters
  - Private fields: `_camelCase` (underscore prefix)
  - Private static fields: `s_camelCase`
  - Interfaces: `IPascalCase` (I prefix)
- Use **file-scoped namespaces** (preferred)
- Place opening braces on new lines

### Code Preferences
- **Avoid `var`** - Use explicit types for built-in types and when type is not apparent
- Use **expression-bodied members** for properties and accessors (when simple)
- Prefer **pattern matching** over type checks and casts
- Use **null-conditional operators** (`?.`) and **null-coalescing** (`??`)
- Use **collection initializers** and **object initializers** when appropriate
- Prefer **foreach** over traditional for loops when possible

### Using Directives
- Place using directives **outside namespace**
- Sort system directives first
- Separate import directive groups
- **Unused using statements are errors** (IDE0005)

### Documentation
- Add XML documentation comments for public APIs
- Include `<summary>`, `<param>`, and `<returns>` tags as appropriate

## Testing Guidelines

- Tests are located in the `Tests/` directory
- Test project naming: `{LibraryName}.Tests`
- Use xUnit for testing framework (if present in existing tests)
- Aim for high test coverage of public APIs
- Write unit tests that are isolated and fast
- Use meaningful test method names that describe what is being tested

## Dependencies and Packages

- Target framework: **.NET 9.0**
- Keep dependencies up to date
- Add new dependencies thoughtfully, considering maintenance and security

## Best Practices

1. **Code Quality**
   - Run `dotnet format` before committing
   - Ensure all tests pass before submitting PRs
   - Follow existing patterns in the codebase

2. **Security**
   - Never commit secrets, API keys, or credentials
   - Use secure coding practices for authentication and data handling
   - Validate and sanitize all external inputs

3. **Performance**
   - Consider async/await patterns for I/O operations
   - Avoid blocking calls in async methods
   - Use appropriate data structures for the task

4. **Maintainability**
   - Keep methods focused and single-purpose
   - Extract complex logic into well-named helper methods
   - Add comments for non-obvious business logic

## Common Tasks

### Adding a New Library
1. Create project in `Libraries/` directory
2. Follow existing project structure and naming
3. Add corresponding test project in `Tests/`
4. Update solution file if needed
5. Add README.md with package documentation

### Adding a New Sample
1. Create project in `Samples/` directory
2. Include clear README with setup instructions
3. Demonstrate best practices for SDK usage
4. Keep samples simple and focused

### Making API Changes
1. Maintain backward compatibility when possible
2. Mark obsolete APIs with `[Obsolete]` attribute before removal
3. Update XML documentation
4. Add/update tests for new functionality
5. Update relevant README files

## CI/CD

The repository uses GitHub Actions for continuous integration:
- **build-test-lint.yml** - Runs on PRs and pushes to main
- **codeql.yml** - Security scanning
- All changes must pass CI checks before merging

## Contact

For questions or feedback: TeamsAISDKFeedback@microsoft.com
