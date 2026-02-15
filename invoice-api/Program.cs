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

// Readiness check
app.MapGet("/ready", () => Results.Ok(new { status = "ready", timestamp = DateTime.Now }));

// Customer endpoints
app.MapGet("/customers", async (ICustomerRepository customerRepo) =>
{
    var customers = await customerRepo.GetAllAsync();
    return Results.Ok(ResultHelper.Success(customers.Select(c => c.ToResponseModel())));
});

app.MapGet("/customers/{id}", async (string id, ICustomerRepository customerRepo) =>
{
    var customer = await customerRepo.GetByIdAsync(id);
    return customer == null
        ? Results.NotFound()
        : Results.Ok(ResultHelper.Success(customer.ToResponseModel()));
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
    return Results.Ok(ResultHelper.Success(existingCustomer.ToResponseModel()));
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
    var invoice = await invoiceRepo.GetByOnlyIdAsync(id);
    return invoice == null
        ? Results.NotFound()
        : Results.Ok(ResultHelper.Success(invoice.ToResponseModel()));
});

app.MapPost("/invoices", async (CreateInvoiceRequest request, IInvoiceRepository invoiceRepo) =>
{
    if (!ValidationHelper.IsValidInvoice(request))
    {
        return Results.BadRequest("Invalid invoice data");
    }

    // 时间戳 + 一个4位随机数补齐 (防止同一毫秒内重复)
    var timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
    var randomPart = new Random().Next(1000, 9999);
    var invoiceId = $"{timeStamp}{randomPart}";

    var invoice = new Invoice(request.CustomerId,  invoiceId, request.IssueDate)
    {
        CustomerName = request.CustomerName,
        IssueDate = request.IssueDate.ToString("yyyy-MM-dd"),
        Amount = request.Amount,
        DeliveryAddress = request.DeliveryAddress,
    };

    await invoiceRepo.CreateAsync(invoice);
    return Results.Created($"/invoices/{invoice.CustomerId}/invoices/{invoiceId}/{invoice.IssueDate}", invoice);
});

app.MapPut("/invoices/{id}", async (string id, UpdateInvoiceRequest request, IInvoiceRepository invoiceRepo) =>
{
    if (id != request.InvoiceId || !ValidationHelper.IsValidUpdateInvoice(request))
    {
        return Results.BadRequest(ResultHelper.BadRequest("Invalid invoice data"));
    }

    var existingInvoice = await invoiceRepo.GetByOnlyIdAsync(id);
    if (existingInvoice == null)
    {
        return Results.NotFound();
    }

    // Only these fields can be updated
    existingInvoice.CustomerName = request.CustomerName;
    existingInvoice.Amount = request.Amount;
    existingInvoice.DeliveryAddress = request.DeliveryAddress;

    await invoiceRepo.UpdateAsync(existingInvoice);
    return Results.Ok(ResultHelper.Success(existingInvoice.ToResponseModel()));
});

app.MapDelete("/invoices/{id}", async (string id, IInvoiceRepository invoiceRepo) =>
{
    var existingInvoice = await invoiceRepo.GetByOnlyIdAsync(id);
    if (existingInvoice == null)
    {
        return Results.NotFound();
    }

    await invoiceRepo.DeleteByKeyAsync(existingInvoice.PartitionKey, existingInvoice.SortKey);
    return Results.NoContent();
});

app.MapGet("/customers/{customerId}/invoices", async (string customerId, IInvoiceRepository invoiceRepo) =>
{
    var customerInvoices = await invoiceRepo.GetByCustomerAsync(customerId);
    return Results.Ok(ResultHelper.Success(customerInvoices.Select(c => c.ToResponseModel())));
});

app.MapGet(
    "/customer/{customerId}/invoices/{invoiceId}/{issueDate}",
    async (string customerId, string invoiceId, string issueDate, IInvoiceRepository invoiceRepo) =>
    {
        if (!DateTime.TryParse(issueDate, out DateTime date))
        {
            return Results.BadRequest(ResultHelper.BadRequest("非法的发票日期格式"));
        }

        var invoice = await invoiceRepo.GetByIdAsync(customerId, invoiceId, date.ToString("yyyy-MM-dd"));
        return invoice == null
            ? Results.NotFound()
            : Results.Ok(ResultHelper.Success(invoice.ToResponseModel()));
    });

app.Run();