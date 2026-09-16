fmt:
	dotnet format

build:
	dotnet build

test:
	dotnet test -v d

test_cov:
	dotnet test -v d --collect:"XPlat Code Coverage"

test_report:
	reportgenerator -reporttypes:Html -reports:**/coverage.cobertura.xml -targetdir:TestCoverage

clean:
	dotnet clean

samples_core_run:
	dotnet run --project samples/CoreBot

samples_teams_run:
	dotnet run --project samples/TeamsBot

samples_mcp_run:
	dotnet run --project samples/McpServer