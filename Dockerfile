# ── Build stage ───────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TheFabricScript.sln .
COPY TheFabricScript.Core/TheFabricScript.Core.csproj TheFabricScript.Core/
COPY TheFabricScript.Infrastructure/TheFabricScript.Infrastructure.csproj TheFabricScript.Infrastructure/
COPY TheFabricScript.API/TheFabricScript.API.csproj TheFabricScript.API/
COPY TheFabricScript.Tests/TheFabricScript.Tests.csproj TheFabricScript.Tests/

RUN dotnet restore

COPY . .
RUN dotnet publish TheFabricScript.API/TheFabricScript.API.csproj \
    -c Release -o /app/publish --no-restore

# ── Runtime stage ─────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "TheFabricScript.API.dll"]
