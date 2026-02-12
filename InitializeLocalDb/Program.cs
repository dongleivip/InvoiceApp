using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace InitializeLocalDb;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
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
                    TableName = "InvoiceAppDev"
                };
                await client.DescribeTableAsync(describeRequest);
                Console.WriteLine("Table 'InvoiceAppDev' already exists.");
                return;
            }
            catch (ResourceNotFoundException)
            {
                // Table doesn't exist, continue to create it
                Console.WriteLine("Table 'InvoiceAppDev' does not exist. Creating...");
            }

            // Create table request
            var createTableRequest = new CreateTableRequest
            {
                TableName = "InvoiceAppDev",
                KeySchema = new List<KeySchemaElement>
                {
                    new KeySchemaElement { AttributeName = "pk", KeyType = KeyType.HASH },
                    new KeySchemaElement { AttributeName = "sk", KeyType = KeyType.RANGE }
                },
                AttributeDefinitions = new List<AttributeDefinition>
                {
                    new AttributeDefinition { AttributeName = "pk", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "sk", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "gsi_pk", AttributeType = ScalarAttributeType.S },
                    new AttributeDefinition { AttributeName = "gsi_sk", AttributeType = ScalarAttributeType.S }
                },
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
                {
                    new GlobalSecondaryIndex
                    {
                        IndexName = "GSI1",
                        KeySchema = new List<KeySchemaElement>
                        {
                            new KeySchemaElement { AttributeName = "gsi_pk", KeyType = KeyType.HASH },
                            new KeySchemaElement { AttributeName = "gsi_sk", KeyType = KeyType.RANGE }
                        },
                        Projection = new Projection { ProjectionType = ProjectionType.ALL },
                        ProvisionedThroughput = new ProvisionedThroughput
                        {
                            ReadCapacityUnits = 5,
                            WriteCapacityUnits = 5
                        }
                    }
                },
                BillingMode = BillingMode.PAY_PER_REQUEST
            };

            var response = await client.CreateTableAsync(createTableRequest);

            // Wait for table to be active
            Console.WriteLine("Waiting for table to become active...");
            await WaitForTableToBecomeActive(client, "InvoiceAppDev");

            Console.WriteLine("✅ DynamoDB table 'InvoiceAppDev' created successfully!");
            Console.WriteLine($"Table Status: {response.TableDescription.TableStatus}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"❌ Error creating DynamoDB table: {ex.Message}");
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
