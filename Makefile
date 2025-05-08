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

samples_echo_run:
	dotnet run --project Samples/Samples.Echo

samples_auth_run:
	dotnet run --project Samples/Samples.Auth

samples_lights_run:
	dotnet run --project Samples/Samples.Lights

samples_mcp_run:
	dotnet run --project Samples/Samples.Mcp