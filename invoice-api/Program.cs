using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;
using InvoiceApi.DTO;
using InvoiceApi.Models;
using InvoiceApi.Repositories;
using InvoiceApi.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add AWS Lambda support
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

// Add DynamoDB service: 注册底层 AWS SDK 客户端
builder.Services.AddScoped<IAmazonDynamoDB>(provider =>
{
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    var configuration = provider.GetRequiredService<IConfiguration>();
    var logger = provider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Initializing DynamoDB client. Environment: {Environment}", environment ?? "Not set");

    // Common configuration for both environments
    var config = new AmazonDynamoDBConfig();

    // Set region
    var region = configuration["AWS:Region"] ?? throw new Exception("AWS Region not set");
    config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
    logger.LogInformation("AWS Region: {Region}", config.RegionEndpoint.SystemName);

    // For development environment
    if (environment == "Development")
    {
        // Set LocalStack service URL
        var serviceUrl = configuration["DynamoDB:ServiceURL"] ?? throw new Exception("DynamoDB Service URL not set");
        config.ServiceURL = serviceUrl;
        logger.LogInformation("DynamoDB Service URL: {ServiceURL}", config.ServiceURL);
    }

    // Get credentials from configuration
    var accessKey = configuration["AWS:AccessKey"] ?? throw new Exception("AWS Access Key not set");
    var secretKey = configuration["AWS:SecretKey"] ?? throw new Exception("AWS Secret Key not set");

    var credentials = new BasicAWSCredentials(accessKey, secretKey);
    return new AmazonDynamoDBClient(credentials, config);
});

// Configure repositories
// 注册 DynamoDBContext (用于对象映射)
builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>(sp =>
{
    var client = sp.GetRequiredService<IAmazonDynamoDB>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var prefix = configuration["DynamoDb:TableNamePrefix"] ?? throw new Exception("DynamoDB TableNamePrefix not set");

    var dbContext = new DynamoDBContextBuilder()
        .WithDynamoDBClient(() => client)
        .ConfigureContext(cfg => cfg.TableNamePrefix = prefix)
        .Build();

    return dbContext;
});

// 注册通用泛型仓储
builder.Services.AddScoped(typeof(IDynamoRepository<>), typeof(DynamoRepository<>));

// 注册特定业务仓储
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();

var app = builder.Build();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Enhanced health check endpoint
app.MapGet("/healthz", async (ICustomerRepository customerRepo) =>
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
            database = "connected",
        });
    }
    catch
    {
        return Results.StatusCode(503);
    }
});

// Customer endpoints
app.MapGet("/customers", async (ICustomerRepository customerRepo) =>
{
    var customers = await customerRepo.GetAllAsync();
    return Results.Ok(ResultHelper.Success(customers));
});

app.MapGet("/customers/{id}", async (string id, ICustomerRepository customerRepo) =>
{
    var customer = await customerRepo.GetByIdAsync(id);
    return customer == null ? Results.NotFound() : Results.Ok(customer);
});

app.MapPost("/customers", async (CreateCustomerRequest request, IDynamoRepository<Customer> customerRepo) =>
{
    if (!ValidationHelper.IsValidCustomer(request))
    {
        return Results.BadRequest(ResultHelper.BadRequest("Invalid customer data"));
    }

    // 1. 生成唯一 ID
    // 使用 N 格式 (无连字符) 使 ID 更简洁，例如 "46f38742..."
    var customerId = Guid.NewGuid().ToString("N");

    // 2. 映射 DTO 到实体
    // 构造函数会处理单表设计的 PK/SK/GSI1 逻辑
    // (可选) 处理 GSI2 如果希望通过其他字段查询
    var customer = new Customer(customerId)
    {
        Name = request.name,
        Contact = request.contact,
        Address = request.address ?? string.Empty,
        TaxId = request.taxId ?? string.Empty,
    };

    await customerRepo.CreateAsync(customer);
    return Results.Created($"/customers/{customer.Id}", customer);
});

app.MapPut("/customers/{id}", async (string id, CreateCustomerRequest customer, ICustomerRepository customerRepo) =>
{
    if (id != customer.id)
    {
        return Results.BadRequest();
    }

    if (!ValidationHelper.IsValidCustomer(customer))
    {
        return Results.BadRequest("Invalid customer data");
    }

    var existingCustomer = await customerRepo.GetByIdAsync(id);
    if (existingCustomer == null)
    {
        return Results.NotFound();
    }

    existingCustomer.Name = customer.name;
    existingCustomer.Contact = customer.contact;
    existingCustomer.Address = customer.address;
    existingCustomer.TaxId = customer.taxId;

    await customerRepo.UpdateAsync(existingCustomer);
    return Results.Ok(ResultHelper.Success(existingCustomer));
});

app.MapDelete("/customers/{id}", async (string id, ICustomerRepository customerRepo) =>
{
    var existingCustomer = await customerRepo.GetByIdAsync(id);
    if (existingCustomer == null)
    {
        return Results.NotFound();
    }

    await customerRepo.DeleteAsync(existingCustomer.PartitionKey, existingCustomer.SortKey);
    return Results.NoContent();
});

// Invoice endpoints
app.MapGet("/invoices/{id}", async (string id, IInvoiceRepository invoiceRepo) =>
{
    var invoice = await invoiceRepo.GetByIdAsync(id);
    if (invoice == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(ResultHelper.Success(invoice));
});

app.MapPost("/invoices", async (Invoice invoice, IInvoiceRepository invoiceRepo) =>
{
    if (!ValidationHelper.IsValidInvoice(invoice))
    {
        return Results.BadRequest("Invalid invoice data");
    }

    if (string.IsNullOrEmpty(invoice.Id))
    {
        invoice.Id = Guid.NewGuid().ToString();
    }

    await invoiceRepo.CreateAsync(invoice);
    return Results.Created($"/invoices/{invoice.Id}", invoice);
});

app.MapPut("/invoices/{id}", async (string id, Invoice invoice, IInvoiceRepository invoiceRepo) =>
{
    if (id != invoice.Id)
    {
        return Results.BadRequest();
    }

    if (!ValidationHelper.IsValidInvoice(invoice))
    {
        return Results.BadRequest("Invalid invoice data");
    }

    var existingInvoice = await invoiceRepo.GetByIdAsync(id);
    if (existingInvoice == null)
    {
        return Results.NotFound();
    }

    await invoiceRepo.UpdateAsync(invoice);
    return Results.Ok(ResultHelper.Success(invoice));
});

app.MapDelete("/invoices/{id}", async (string id, IInvoiceRepository invoiceRepo) =>
{
    var existingInvoice = await invoiceRepo.GetByIdAsync(id);
    if (existingInvoice == null)
    {
        return Results.NotFound();
    }

    await invoiceRepo.DeleteAsync(existingInvoice.PartitionKey, existingInvoice.SortKey);
    return Results.NoContent();
});

// Get invoices for a specific customer
app.MapGet("/customers/{customerId}/invoices", async (string customerId, IInvoiceRepository invoiceRepo) =>
{
    var customerInvoices = await invoiceRepo.GetCustomerInvoicesAsync(customerId);
    return Results.Ok(ResultHelper.Success(customerInvoices));
});

app.Run();