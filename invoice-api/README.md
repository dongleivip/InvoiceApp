# Invoice API

This is a serverless API built with .NET 9 Minimal API and deployed on AWS Lambda. It uses DynamoDB with a single-table
design to store customer and invoice data.

## Architecture

- **Backend**: .NET 9 Minimal API
- **Deployment**: AWS Lambda
- **Database**: Amazon DynamoDB with Single Table Design
- **API Gateway**: REST API

## Features

- Customer management (CRUD operations)
- Invoice management (CRUD operations)
- Query invoices by customer
- Serverless architecture for cost efficiency

## DynamoDB Single Table Design

The solution uses a single DynamoDB table with the following structure:

- `pk` (Partition Key): `{ENTITY_TYPE}#{ID}` (e.g., `CUSTOMER#123`, `INVOICE#456`)
- `sk` (Sort Key): `{ENTITY_TYPE}#{ID}` (e.g., `CUSTOMER#123`, `INVOICE#456`)
- `gsi_pk` (GSI Partition Key): Used for querying invoices by customer
- `gsi_sk` (GSI Sort Key): Used for ordering items in GSI
- `entity_type`: "CUSTOMER" or "INVOICE"

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [AWS CLI](https://aws.amazon.com/cli/) configured with appropriate credentials
- [AWS SAM CLI](https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/serverless-sam-cli-install.html) (
  optional)

## Installation

1. Clone the repository:

```bash
git clone <repository-url>
cd invoice-api
```

2. Restore dependencies:

```bash
dotnet restore
```

## Local Development

For local development, you can use AWS SAM or run the application directly with mock DynamoDB:

1. Install and run DynamoDB Local:

```bash
docker run -p 8000:8000 amazon/dynamodb-local
```

2. Set environment variables:

```bash
export DYNAMODB_ENDPOINT=http://localhost:8000
export DYNAMODB_TABLE_NAME=InvoiceApp-Local
```

3. Run the application:

```bash
dotnet run
```

## Deployment to AWS Lambda

1. Publish the application to AWS Lambda:

```bash
dotnet lambda deploy-function InvoiceApi
```

Or using AWS SAM:

```bash
sam build
sam deploy --guided
```

2. The API will be accessible via the API Gateway endpoint provided after deployment.

## API Endpoints

### Customer Endpoints

- `GET /customers` - Get all customers
- `GET /customers/{id}` - Get a specific customer
- `POST /customers` - Create a new customer
- `PUT /customers/{id}` - Update a customer
- `DELETE /customers/{id}` - Delete a customer

### Invoice Endpoints

- `GET /invoices` - Get all invoices
- `GET /invoices/{id}` - Get a specific invoice
- `POST /invoices` - Create a new invoice
- `PUT /invoices/{id}` - Update an invoice
- `DELETE /invoices/{id}` - Delete an invoice
- `GET /customers/{customerId}/invoices` - Get all invoices for a specific customer

## Request/Response Examples

### Create Customer

```json
POST /customers
{
  "name": "John Doe",
  "email": "john@example.com",
  "address": "123 Main St, City, State"
}
```

### Create Invoice

```json
POST /invoices
{
  "customerId": "customer-id-123",
  "customerName": "John Doe",
  "amount": 150.00,
  "issueDate": "2023-01-15T00:00:00Z"
}
```

## Environment Variables

- `DYNAMODB_TABLE_NAME` - Name of the DynamoDB table (default: InvoiceApp)

## Security

- Ensure proper IAM roles and policies are configured for Lambda function to access DynamoDB
- Use AWS Secrets Manager for storing sensitive configuration if needed

## Cost Optimization

- Lambda pricing is based on requests and duration
- DynamoDB offers flexible pricing based on read/write capacity
- Monitor usage to optimize costs

## Troubleshooting

- Check CloudWatch logs for Lambda function logs
- Verify IAM permissions for DynamoDB access
- Ensure DynamoDB table is created with the correct schema