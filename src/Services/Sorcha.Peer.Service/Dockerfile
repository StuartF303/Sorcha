# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Services/Sorcha.Peer.Service/Sorcha.Peer.Service.csproj", "src/Services/Sorcha.Peer.Service/"]
COPY ["src/Services/Sorcha.Register.Service/Sorcha.Register.Service.csproj", "src/Services/Sorcha.Register.Service/"]
COPY ["src/Common/Sorcha.ServiceDefaults/Sorcha.ServiceDefaults.csproj", "src/Common/Sorcha.ServiceDefaults/"]
COPY ["src/Common/Sorcha.Blueprint.Models/Sorcha.Blueprint.Models.csproj", "src/Common/Sorcha.Blueprint.Models/"]
COPY ["src/Core/Sorcha.Register.Storage/Sorcha.Register.Storage.csproj", "src/Core/Sorcha.Register.Storage/"]
COPY ["src/Common/Sorcha.TransactionHandler/Sorcha.TransactionHandler.csproj", "src/Common/Sorcha.TransactionHandler/"]
COPY ["src/Common/Sorcha.Cryptography/Sorcha.Cryptography.csproj", "src/Common/Sorcha.Cryptography/"]

# Restore dependencies
RUN dotnet restore "src/Services/Sorcha.Peer.Service/Sorcha.Peer.Service.csproj"

# Copy source code (including proto files)
COPY src/ ./src/

# Build the application
WORKDIR "/src/src/Services/Sorcha.Peer.Service"
RUN dotnet build "Sorcha.Peer.Service.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Sorcha.Peer.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:ErrorOnDuplicatePublishOutputFiles=false

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

ENTRYPOINT ["dotnet", "Sorcha.Peer.Service.dll"]
