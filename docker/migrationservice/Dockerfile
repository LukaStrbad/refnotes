FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ./src/Data/packages.lock.json ./src/Data/Data.csproj ./Data/
RUN dotnet restore ./Data/ --locked-mode

COPY ./src/ServiceDefaults/ServiceDefaults.csproj ./ServiceDefaults/
# No --locked-mode as this project doesn't have a packages.lock.json
RUN dotnet restore ./ServiceDefaults/

COPY ./src/MigrationService/packages.lock.json ./src/MigrationService/MigrationService.csproj ./MigrationService/
RUN dotnet restore ./MigrationService/ --locked-mode

# Copy the other files
COPY ./src/Data ./Data/
COPY ./src/ServiceDefaults ./ServiceDefaults/
COPY ./src/MigrationService ./MigrationService/

# Build the project
RUN dotnet publish ./MigrationService --no-restore -c Release -r linux-x64 -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MigrationService.dll"]
