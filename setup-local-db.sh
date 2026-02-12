#!/bin/bash

set -euo pipefail

# Wait for LocalStack to be ready
echo "Waiting for LocalStack to be ready..."
until curl -s http://localhost:4566/_localstack/health | grep -q '"dynamodb": "available"'; do
  echo "waiting for dynamodb to be available ..."
  sleep 1
done

echo "LocalStack is ready. Creating DynamoDB table..."

# Create DynamoDB table for Invoice App
aws --endpoint-url=http://localhost:4566 dynamodb create-table \
    --table-name InvoiceAppDev \
    --attribute-definitions \
        AttributeName=pk,AttributeType=S \
        AttributeName=sk,AttributeType=S \
        AttributeName=gsi_pk,AttributeType=S \
        AttributeName=gsi_sk,AttributeType=S \
    --key-schema \
        AttributeName=pk,KeyType=HASH \
        AttributeName=sk,KeyType=RANGE \
    --global-secondary-indexes \
        '[
            {
                "IndexName": "GSI1",
                "KeySchema": [
                    {"AttributeName":"gsi_pk","KeyType":"HASH"},
                    {"AttributeName":"gsi_sk","KeyType":"RANGE"}
                ],
                "Projection": {
                    "ProjectionType":"ALL"
                },
                "ProvisionedThroughput": {
                    "ReadCapacityUnits": 5,
                    "WriteCapacityUnits": 5
                }
            }
        ]' \
    --billing-mode PAY_PER_REQUEST

echo "DynamoDB table created successfully!"