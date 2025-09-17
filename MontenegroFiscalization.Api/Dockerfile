# MontenegroFiscalization.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copying project files
COPY ["MontenegroFiscalization.Api/MontenegroFiscalization.Api.csproj", "MontenegroFiscalization.Api/"]
COPY ["MontenegroFiscalization.Core/MontenegroFiscalization.Core.csproj", "MontenegroFiscalization.Core/"]
COPY ["MontenegroFiscalization.Infrastructure/MontenegroFiscalization.Infrastructure.csproj", "MontenegroFiscalization.Infrastructure/"]

# Restoring dependencies
RUN dotnet restore "MontenegroFiscalization.Api/MontenegroFiscalization.Api.csproj"

# Copying everything else
COPY . .

# Building
WORKDIR "/src/MontenegroFiscalization.Api"
RUN dotnet build "MontenegroFiscalization.Api.csproj" -c Release -o /app/build

# Publishing
FROM build AS publish
RUN dotnet publish "MontenegroFiscalization.Api.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Creating certificates directory
RUN mkdir -p /app/Certificates

# for health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "MontenegroFiscalization.Api.dll"]