# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src

# Copy project file and restore
COPY *.csproj ./
RUN dotnet restore

# Copy all files and publish
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=builder /app/publish .

# Install git for repository operations
RUN apt-get update && \
    apt-get install -y git && \
    rm -rf /var/lib/apt/lists/*

# Entrypoint for the CLI
ENTRYPOINT ["dotnet", "ef-migration-diff.dll"]
