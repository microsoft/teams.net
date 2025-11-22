 # dotnet publish /t:PublishContainer ../Samples/Samples.Echo/Samples.Echo.csproj
 docker run -it --env-file .env -p 3978:3978 samples-echo