#!/bin/bash

set -euo pipefail


# Wait for LocalStack to be ready
echo "Waiting for LocalStack to be ready..."
until curl -s http://localhost:4566/_localstack/health | grep -q '"dynamodb": "available"'; do
  echo "waiting for dynamodb to be available ..."
  sleep 1
done

echo "LocalStack is ready. Initializing DynamoDB table..."

# Build and run the .NET initialization tool
cd /app/InitializeLocalDb
dotnet run

echo "DynamoDB initialization completed!"
