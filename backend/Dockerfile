# syntax=docker/dockerfile:1
# Build context = this `backend/` directory.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/MyQuiza.Api/MyQuiza.Api.csproj src/MyQuiza.Api/
RUN dotnet restore src/MyQuiza.Api/MyQuiza.Api.csproj
COPY . .
RUN dotnet publish src/MyQuiza.Api/MyQuiza.Api.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# Render/Azure inject $PORT; fall back to 8080 locally.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet MyQuiza.Api.dll"]
