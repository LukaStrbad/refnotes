# syntax=docker/dockerfile:1.7-labs

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY --exclude=./src/Web ./src/ ./
RUN dotnet restore --locked-mode ./MigrationService/

RUN dotnet publish ./MigrationService --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MigrationService.dll"]
