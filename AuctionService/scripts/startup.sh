#!/bin/bash
# AuctionService startup script
# Runs EF migrations and starts the application

set -e

echo "Running database migrations..."
export PATH="$PATH:/root/.dotnet/tools"
/root/.dotnet/tools/dotnet-ef database update --project AuctionService.Api.csproj --no-build

echo "Starting AuctionService..."
exec dotnet AuctionService.Api.dll