# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restoration
COPY ["src/Apps/Sorcha.UI/Sorcha.UI.Web/Sorcha.UI.Web.csproj", "Apps/Sorcha.UI/Sorcha.UI.Web/"]
COPY ["src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Sorcha.UI.Web.Client.csproj", "Apps/Sorcha.UI/Sorcha.UI.Web.Client/"]
COPY ["src/Apps/Sorcha.UI/Sorcha.UI.Shared/Sorcha.UI.Shared.csproj", "Apps/Sorcha.UI/Sorcha.UI.Shared/"]
COPY ["src/Apps/Sorcha.UI/Sorcha.UI.Core/Sorcha.UI.Core.csproj", "Apps/Sorcha.UI/Sorcha.UI.Core/"]

# Restore dependencies
RUN dotnet restore "Apps/Sorcha.UI/Sorcha.UI.Web/Sorcha.UI.Web.csproj"

# Copy source code
COPY src/ .

# Build and publish the application
WORKDIR "/src/Apps/Sorcha.UI/Sorcha.UI.Web"
RUN dotnet publish "Sorcha.UI.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - use ASP.NET Core runtime for Blazor Web App
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published application
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "Sorcha.UI.Web.dll"]
