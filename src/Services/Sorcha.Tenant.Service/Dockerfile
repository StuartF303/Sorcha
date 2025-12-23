# SPDX-License-Identifier: MIT
# Copyright (c) 2025 Sorcha Contributors

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["src/Services/Sorcha.Tenant.Service/Sorcha.Tenant.Service.csproj", "src/Services/Sorcha.Tenant.Service/"]
COPY ["src/Common/Sorcha.ServiceDefaults/Sorcha.ServiceDefaults.csproj", "src/Common/Sorcha.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "src/Services/Sorcha.Tenant.Service/Sorcha.Tenant.Service.csproj"

# Copy source code
COPY src/ ./src/

# Build the application
WORKDIR "/src/src/Services/Sorcha.Tenant.Service"
RUN dotnet build "Sorcha.Tenant.Service.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Sorcha.Tenant.Service.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Dependencies stage - Install native libraries
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# Final stage - Ubuntu Chiseled (distroless)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app

# Copy Kerberos libraries from dependencies stage
COPY --from=dependencies /usr/lib/x86_64-linux-gnu/libgssapi_krb5.so.2* /usr/lib/x86_64-linux-gnu/
COPY --from=dependencies /usr/lib/x86_64-linux-gnu/libkrb5.so.3* /usr/lib/x86_64-linux-gnu/
COPY --from=dependencies /usr/lib/x86_64-linux-gnu/libk5crypto.so.3* /usr/lib/x86_64-linux-gnu/
COPY --from=dependencies /usr/lib/x86_64-linux-gnu/libcom_err.so.2* /usr/lib/x86_64-linux-gnu/
COPY --from=dependencies /usr/lib/x86_64-linux-gnu/libkrb5support.so.0* /usr/lib/x86_64-linux-gnu/
COPY --from=dependencies /lib/x86_64-linux-gnu/libkeyutils.so.1* /lib/x86_64-linux-gnu/
COPY --from=dependencies /lib/x86_64-linux-gnu/libresolv.so.2* /lib/x86_64-linux-gnu/

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

ENTRYPOINT ["dotnet", "Sorcha.Tenant.Service.dll"]
