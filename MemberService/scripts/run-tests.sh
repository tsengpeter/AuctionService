#!/bin/bash

# Run tests script for MemberService

set -e

echo "Running tests..."

# Run all tests
dotnet test --configuration Release --verbosity normal

# Run with coverage (if dotnet-coverage is installed)
# dotnet-coverage collect "dotnet test" --output coverage.json --output-format json

echo "Tests completed!"