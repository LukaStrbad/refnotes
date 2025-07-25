FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ./src/Data/packages.lock.json ./src/Data/Data.csproj ./Data/
RUN dotnet restore ./Data/ --locked-mode

COPY ./src/ServiceDefaults/ServiceDefaults.csproj ./ServiceDefaults/
# No --locked-mode as this project doesn't have a packages.lock.json
RUN dotnet restore ./ServiceDefaults/

COPY ./src/Api/packages.lock.json ./src/Api/Api.csproj ./Api/
RUN dotnet restore ./Api/ --locked-mode

# Copy the other files
COPY ./src/Data ./Data/
COPY ./src/ServiceDefaults ./ServiceDefaults/
COPY ./src/Api ./Api/

# Build the project
RUN dotnet publish ./Api --no-restore -c Release -r linux-x64 -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "Api.dll"]
