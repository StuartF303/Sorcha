# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Apps/Services/Sorcha.ApiGateway/Sorcha.ApiGateway.csproj", "src/Apps/Services/Sorcha.ApiGateway/"]
COPY ["src/Common/Sorcha.ServiceDefaults/Sorcha.ServiceDefaults.csproj", "src/Common/Sorcha.ServiceDefaults/"]
RUN dotnet restore "src/Apps/Services/Sorcha.ApiGateway/Sorcha.ApiGateway.csproj"

# Copy source code and build
COPY src/ ./src/
WORKDIR "/src/src/Apps/Services/Sorcha.ApiGateway"
RUN dotnet build "Sorcha.ApiGateway.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Sorcha.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app

# Copy published app
COPY --from=publish /app/publish .

# Create non-root user (use -o to allow non-unique UID if exists)
RUN id -u appuser &>/dev/null || useradd -m -u 1000 appuser; chown -R $(id -u 1000):$(id -g 1000) /app
USER 1000

# Expose ports
EXPOSE 8080
EXPOSE 8443

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "Sorcha.ApiGateway.dll"]
