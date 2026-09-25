# SDK-образ: исходники монтируются volume'ом, сборка выполняется на старте контейнера.
FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    ASPNETCORE_URLS=http://+:8090 \
    DOTNET_USE_POLLING_FILE_WATCHER=true

EXPOSE 8090

CMD ["sh", "-c", "dotnet restore && dotnet build -c Release -o /out && exec dotnet /out/TestApp.dll"]
