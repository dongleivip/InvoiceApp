using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace InitializeLocalDb;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            const TABLE_NAME = "Dev_InvoiceApp";
            Console.WriteLine("Initializing DynamoDB table for local development...");

            var config = new AmazonDynamoDBConfig
            {
                ServiceURL = "http://localhost:4566",
                UseHttp = true,
                AuthenticationRegion = "us-east-1"
            };

            using var client = new AmazonDynamoDBClient(
                new Amazon.Runtime.BasicAWSCredentials("localstack", "localstack"),
                config);

            // Check if table already exists
            try
            {
                var describeRequest = new DescribeTableRequest
                {
                    TableName = TABLE_NAME
                };
                await client.DescribeTableAsync(describeRequest);
                Console.WriteLine($"Table '{TABLE_NAME}' already exists.");
                return;
            }
            catch (ResourceNotFoundException)
            {
                // Table doesn't exist, continue to create it
                Console.WriteLine($"Table '{TABLE_NAME}' does not exist. Creating...");
            }

            // Create table request
            var createTableRequest = new CreateTableRequest
            {
                TableName = TABLE_NAME,
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "PK", KeyType = KeyType.HASH },
                    new KeySchemaElement { AttributeName = "SK", KeyType = KeyType.RANGE }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "PK", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "SK", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "GSI1PK", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "GSI1SK", AttributeType = ScalarAttributeType.S }
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                {
                    new GlobalSecondaryIndex
                    {
                        IndexName = "GSI1",
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement { AttributeName = "GSI1PK", KeyType = KeyType.HASH },
                            new KeySchemaElement { AttributeName = "GSI1SK", KeyType = KeyType.RANGE }
                        },
                        Projection = new Projection { ProjectionType = ProjectionType.ALL }
                    }
                },
                BillingMode = BillingMode.PAY_PER_REQUEST
            };

            var response = await client.CreateTableAsync(createTableRequest);

            // Wait for table to be active
            Console.WriteLine("Waiting for table to become active...");
            await WaitForTableToBecomeActive(client, TABLE_NAME);

            Console.WriteLine($"DynamoDB table '{TABLE_NAME}' created successfully!");
            Console.WriteLine($"Table Status: {response.TableDescription.TableStatus}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error creating DynamoDB table: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static async Task WaitForTableToBecomeActive(AmazonDynamoDBClient client, string tableName)
    {
        var request = new DescribeTableRequest { TableName = tableName };
        TableStatus status;

        do
        {
            await Task.Delay(1000); // Wait 1 second
            var response = await client.DescribeTableAsync(request);
            status = response.Table.TableStatus;
            Console.WriteLine($"Table status: {status}");
        } while (status != TableStatus.ACTIVE);
    }
}
