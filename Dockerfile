# ========== BUILD STAGE ==========
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY Musicly.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish Musicly.csproj -c Release -o /app/publish

# ========== RUNTIME STAGE ==========
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create directory for uploaded music files
RUN mkdir -p /app/wwwroot/music

COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Musicly.dll"]
