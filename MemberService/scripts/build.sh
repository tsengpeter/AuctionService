#!/bin/bash

# Build script for MemberService

set -e

echo "Building MemberService..."

# Restore packages
dotnet restore

# Build all projects
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release --no-build

# Publish API
dotnet publish src/MemberService.API/MemberService.API.csproj --configuration Release --output publish

echo "Build completed successfully!"