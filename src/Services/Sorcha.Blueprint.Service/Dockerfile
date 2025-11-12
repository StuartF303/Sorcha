# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Apps/Services/Sorcha.Blueprint.Api/Sorcha.Blueprint.Api.csproj", "Apps/Services/Sorcha.Blueprint.Api/"]
COPY ["src/Common/Sorcha.ServiceDefaults/Sorcha.ServiceDefaults.csproj", "Common/Sorcha.ServiceDefaults/"]
COPY ["src/Common/Sorcha.Blueprint.Models/Sorcha.Blueprint.Models.csproj", "Common/Sorcha.Blueprint.Models/"]

# Restore dependencies
RUN dotnet restore "Apps/Services/Sorcha.Blueprint.Api/Sorcha.Blueprint.Api.csproj"

# Copy source code
COPY src/ .

# Build the application
WORKDIR "/src/Apps/Services/Sorcha.Blueprint.Api"
RUN dotnet build "Sorcha.Blueprint.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Sorcha.Blueprint.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage - Ubuntu Chiseled (distroless)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_HTTP_PORTS=8080

# Run as non-root user (chiseled images are minimal and secure)
USER $APP_UID

ENTRYPOINT ["dotnet", "Sorcha.Blueprint.Api.dll"]
