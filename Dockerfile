# --- Stage 1: Build & AOT Publish ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src

# Copy solution and project files first (layer caching)
COPY Bzn.Cloudios.slnx .
COPY src/Bzn.Cloudios.Domain/Bzn.Cloudios.Domain.csproj src/Bzn.Cloudios.Domain/
COPY src/Bzn.Cloudios.Infrastructure/Bzn.Cloudios.Infrastructure.csproj src/Bzn.Cloudios.Infrastructure/
COPY src/Bzn.Cloudios.Application/Bzn.Cloudios.Application.csproj src/Bzn.Cloudios.Application/
COPY src/Bzn.Cloudios.WebAPI/Bzn.Cloudios.WebAPI.csproj src/Bzn.Cloudios.WebAPI/
COPY src/Bzn.Cloudios.WebApp/Bzn.Cloudios.WebApp.csproj src/Bzn.Cloudios.WebApp/
COPY src/Bzn.Cloudios.WebPlatform/Bzn.Cloudios.WebPlatform.csproj src/Bzn.Cloudios.WebPlatform/
RUN dotnet restore Bzn.Cloudios.slnx

# Copy full source
COPY src/ src/
RUN dotnet publish src/Bzn.Cloudios.WebAPI/Bzn.Cloudios.WebAPI.csproj \
    -c Release -r linux-x64 -o /app/publish

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN adduser --disabled-password --gecos "" appuser

# Data directory for SQLite databases
RUN mkdir -p /data && chown appuser:appuser /data

COPY --from=publish /app/publish .
RUN chown -R appuser:appuser /app
USER appuser

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["./Bzn.Cloudios.WebAPI"]
