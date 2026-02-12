using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using InvoiceApi.Models;
using InvoiceApi.Repositories;
using InvoiceApi.Utils;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add AWS Lambda support
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

// Add DynamoDB service
builder.Services.AddScoped<IAmazonDynamoDB>(provider =>
{
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    var configuration = provider.GetRequiredService<IConfiguration>();
    var logger = provider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Initializing DynamoDB client. Environment: {Environment}", environment ?? "Not set");

    // Common configuration for both environments
    var config = new AmazonDynamoDBConfig();

    // Set region
    var region = configuration["DynamoDB:Region"] ?? "us-east-1";
    config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region);
    logger.LogInformation("DynamoDB Region: {Region}", config.RegionEndpoint.SystemName);

    // For development environment
    if (environment == "Development")
    {
        // Set LocalStack service URL
        var serviceUrl = configuration["DynamoDB:ServiceURL"] ?? "http://localhost:4566";
        config.ServiceURL = serviceUrl;
        logger.LogInformation("LocalStack Service URL: {ServiceURL}", config.ServiceURL);
    }

    // Get credentials from configuration
    var accessKey = configuration["AWS:AccessKey"];
    var secretKey = configuration["AWS:SecretKey"];

    // Create DynamoDB client with credentials
    if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
    {
        var credentials = new Amazon.Runtime.BasicAWSCredentials(accessKey, secretKey);
        return new AmazonDynamoDBClient(credentials, config);
    }
    else
    {
        // Fall back to default credential chain (IAM roles, etc.)
        logger.LogInformation("Falling back to default AWS credential chain");
        return new AmazonDynamoDBClient(config);
    }
});

// Configure repositories
builder.Services.AddScoped<IDynamoRepository<Customer>>(provider =>
{
    var dynamoDbClient = provider.GetRequiredService<IAmazonDynamoDB>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new DynamoRepository<Customer>(dynamoDbClient, configuration);
});

builder.Services.AddScoped<IDynamoRepository<Invoice>>(provider =>
{
    var dynamoDbClient = provider.GetRequiredService<IAmazonDynamoDB>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new DynamoRepository<Invoice>(dynamoDbClient, configuration);
});

builder.Services.AddScoped<IInvoiceRepository>(provider =>
{
    var dynamoDbClient = provider.GetRequiredService<IAmazonDynamoDB>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    return new InvoiceRepository(dynamoDbClient, configuration);
});

var app = builder.Build();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Enhanced health check endpoint
app.MapGet("/healthz", async (IDynamoRepository<Customer> customerRepo) =>
{
    try
    {
        // Perform a simple database connectivity check
        await customerRepo.GetAllAsync();
        return Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Invoice API",
            database = "connected"
        });
    }
    catch
    {
        return Results.StatusCode(503);
    }
});

// Customer endpoints
app.MapGet("/customers", async (IDynamoRepository<Customer> customerRepo) =>
{
    var customers = await customerRepo.GetAllAsync();
    return Results.Ok(customers);
});

app.MapGet("/customers/{id}", async (string id, IDynamoRepository<Customer> customerRepo) =>
{
    var customer = await customerRepo.GetByIdAsync(id);
    if (customer == null) return Results.NotFound();
    return Results.Ok(customer);
});

app.MapPost("/customers", async (Customer customer, IDynamoRepository<Customer> customerRepo) =>
{
    if (!ValidationHelper.IsValidCustomer(customer))
        return Results.BadRequest("Invalid customer data");

    if (string.IsNullOrEmpty(customer.Id))
        customer.Id = Guid.NewGuid().ToString();

    await customerRepo.CreateAsync(customer);
    return Results.Created($"/customers/{customer.Id}", customer);
});

app.MapPut("/customers/{id}", async (string id, Customer customer, IDynamoRepository<Customer> customerRepo) =>
{
    if (id != customer.Id) return Results.BadRequest();

    if (!ValidationHelper.IsValidCustomer(customer))
        return Results.BadRequest("Invalid customer data");

    var existingCustomer = await customerRepo.GetByIdAsync(id);
    if (existingCustomer == null) return Results.NotFound();

    await customerRepo.UpdateAsync(customer);
    return Results.Ok(customer);
});

app.MapDelete("/customers/{id}", async (string id, IDynamoRepository<Customer> customerRepo) =>
{
    var existingCustomer = await customerRepo.GetByIdAsync(id);
    if (existingCustomer == null) return Results.NotFound();

    await customerRepo.DeleteAsync(id);
    return Results.NoContent();
});

// Invoice endpoints
app.MapGet("/invoices", async (IDynamoRepository<Invoice> invoiceRepo) =>
{
    var invoices = await invoiceRepo.GetAllAsync();
    return Results.Ok(invoices);
});

app.MapGet("/invoices/{id}", async (string id, IDynamoRepository<Invoice> invoiceRepo) =>
{
    var invoice = await invoiceRepo.GetByIdAsync(id);
    if (invoice == null) return Results.NotFound();
    return Results.Ok(invoice);
});

app.MapPost("/invoices", async (Invoice invoice, IDynamoRepository<Invoice> invoiceRepo) =>
{
    if (!ValidationHelper.IsValidInvoice(invoice))
        return Results.BadRequest("Invalid invoice data");

    if (string.IsNullOrEmpty(invoice.Id))
        invoice.Id = Guid.NewGuid().ToString();

    await invoiceRepo.CreateAsync(invoice);
    return Results.Created($"/invoices/{invoice.Id}", invoice);
});

app.MapPut("/invoices/{id}", async (string id, Invoice invoice, IDynamoRepository<Invoice> invoiceRepo) =>
{
    if (id != invoice.Id) return Results.BadRequest();

    if (!ValidationHelper.IsValidInvoice(invoice))
        return Results.BadRequest("Invalid invoice data");

    var existingInvoice = await invoiceRepo.GetByIdAsync(id);
    if (existingInvoice == null) return Results.NotFound();

    await invoiceRepo.UpdateAsync(invoice);
    return Results.Ok(invoice);
});

app.MapDelete("/invoices/{id}", async (string id, IDynamoRepository<Invoice> invoiceRepo) =>
{
    var existingInvoice = await invoiceRepo.GetByIdAsync(id);
    if (existingInvoice == null) return Results.NotFound();

    await invoiceRepo.DeleteAsync(id);
    return Results.NoContent();
});

// Get invoices for a specific customer
app.MapGet("/customers/{customerId}/invoices", async (string customerId, IInvoiceRepository invoiceRepo) =>
{
    var customerInvoices = await invoiceRepo.GetByCustomerIdAsync(customerId);
    return Results.Ok(customerInvoices);
});

app.Run();