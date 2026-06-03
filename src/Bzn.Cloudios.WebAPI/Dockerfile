FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5021

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Bzn.Cloudios.WebAPI/Bzn.Cloudios.WebAPI.csproj", "Bzn.Cloudios.WebAPI/"]
COPY ["src/Bzn.Cloudios.Application/Bzn.Cloudios.Application.csproj", "Bzn.Cloudios.Application/"]
COPY ["src/Bzn.Cloudios.Domain/Bzn.Cloudios.Domain.csproj", "Bzn.Cloudios.Domain/"]
COPY ["src/Bzn.Cloudios.Infrastructure/Bzn.Cloudios.Infrastructure.csproj", "Bzn.Cloudios.Infrastructure/"]
RUN dotnet restore "Bzn.Cloudios.WebAPI/Bzn.Cloudios.WebAPI.csproj"
COPY src/Bzn.Cloudios.Application Bzn.Cloudios.Application
COPY src/Bzn.Cloudios.Domain Bzn.Cloudios.Domain
COPY src/Bzn.Cloudios.Infrastructure Bzn.Cloudios.Infrastructure
COPY src/Bzn.Cloudios.WebAPI Bzn.Cloudios.WebAPI
RUN dotnet build "Bzn.Cloudios.WebAPI/Bzn.Cloudios.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bzn.Cloudios.WebAPI/Bzn.Cloudios.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/data
ENTRYPOINT ["dotnet", "Bzn.Cloudios.WebAPI.dll"]
