# Stage 1: build the app using the full .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY src/AssetTracker.Api/ ./
RUN dotnet publish -c Release -o /app

# Stage 2: run the app using the smaller runtime-only image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "AssetTracker.Api.dll"]