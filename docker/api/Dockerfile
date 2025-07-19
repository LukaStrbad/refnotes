# syntax=docker/dockerfile:1.7-labs

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY --exclude=./src/Web ./src/ ./
RUN dotnet restore --locked-mode ./Api/

RUN dotnet publish ./Api --no-restore -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "Api.dll"]
