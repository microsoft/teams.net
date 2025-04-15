fmt:
	dotnet format

build:
	dotnet build

test:
	dotnet test

clean:
	dotnet clean

samples_echo_run:
	dotnet run --project Samples/Samples.Echo

samples_auth_run:
	dotnet run --project Samples/Samples.Auth

samples_lights_run:
	dotnet run --project Samples/Samples.Lights