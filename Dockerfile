# Multi-stage build for ef-migration-diff

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /src

COPY ef-migration-diff.csproj .
RUN dotnet restore

COPY . .
RUN dotnet build -c Release --no-restore

RUN dotnet publish -c Release -o /app/publish --no-build

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime:10.0

LABEL maintainer="Vladyslav Zaiets <https://sarmkadan.com>"
LABEL description="Compare Entity Framework migrations between branches"
LABEL version="1.2.0"

WORKDIR /app

COPY --from=builder /app/publish .

# Create workspace directory for volume mount
RUN mkdir -p /workspace
WORKDIR /workspace

# Install git (needed for Git operations)
RUN apt-get update && \
    apt-get install -y git && \
    rm -rf /var/lib/apt/lists/*

# Set entrypoint
ENTRYPOINT ["dotnet", "/app/ef-migration-diff.dll"]

# Default command
CMD ["--help"]

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD dotnet /app/ef-migration-diff.dll --version || exit 1
