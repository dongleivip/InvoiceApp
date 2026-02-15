using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;

namespace Microsoft.Extensions.DependencyInjection;

using InvoiceApi.Repositories;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add DynamoDB Client (注册底层 AWS SDK 客户端)
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddDynamoDbClient(this IServiceCollection services)
    {
        services.AddScoped<IAmazonDynamoDB>(provider =>
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

        return services;
    }

    /// <summary>
    ///  添加 DynamoDBContext.
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddDynamoDbContext(this IServiceCollection services)
    {
        services.AddScoped<IDynamoDBContext, DynamoDBContext>(sp =>
        {
            var client = sp.GetRequiredService<IAmazonDynamoDB>();
            var configuration = sp.GetRequiredService<IConfiguration>();
            var prefix = configuration["DynamoDb:TableNamePrefix"] ??
                         throw new Exception("DynamoDB TableNamePrefix not set");

            var dbContext = new DynamoDBContextBuilder()
                .WithDynamoDBClient(() => client)
                .ConfigureContext(cfg => cfg.TableNamePrefix = prefix)
                .Build();

            return dbContext;
        });
        return services;
    }

    /// <summary>
    ///  添加通用 DynamoDB 仓储服务.
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddGenericDynamoRepository(this IServiceCollection services)
    {
        services.AddScoped(typeof(IDynamoRepository<>), typeof(DynamoRepository<>));
        return services;
    }

    /// <summary>
    /// 添加所有业务仓储服务.
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddBusinessRepositories(this IServiceCollection services)
    {
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        return services;
    }
}