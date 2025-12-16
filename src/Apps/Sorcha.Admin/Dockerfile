# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Apps/Sorcha.Admin/Sorcha.Admin.csproj", "Apps/Sorcha.Admin/"]
COPY ["src/Common/Sorcha.Blueprint.Models/Sorcha.Blueprint.Models.csproj", "Common/Sorcha.Blueprint.Models/"]
COPY ["src/Core/Sorcha.Blueprint.Fluent/Sorcha.Blueprint.Fluent.csproj", "Core/Sorcha.Blueprint.Fluent/"]
COPY ["src/Core/Sorcha.Blueprint.Schemas/Sorcha.Blueprint.Schemas.csproj", "Core/Sorcha.Blueprint.Schemas/"]

# Restore dependencies
RUN dotnet restore "Apps/Sorcha.Admin/Sorcha.Admin.csproj"

# Copy source code
COPY src/ .

# Build and publish the application
WORKDIR "/src/Apps/Sorcha.Admin"
RUN dotnet publish "Sorcha.Admin.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage - serve Blazor WASM with nginx
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Copy published Blazor WASM files
COPY --from=build /app/publish/wwwroot .

# Copy custom nginx configuration
COPY src/Apps/Sorcha.Admin/nginx.conf /etc/nginx/nginx.conf

# Expose port 8080
EXPOSE 8080

# Run nginx
CMD ["nginx", "-g", "daemon off;"]
