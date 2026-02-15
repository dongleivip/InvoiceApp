namespace Microsoft.Extensions.DependencyInjection;

using Amazon.DynamoDBv2.DataModel;
using InvoiceApi.Models;
using InvoiceApi.Repositories;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     添加通用 DynamoDB 仓储服务.
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddDynamoRepository<T>(this IServiceCollection services)
        where T : DataEntity, new()
    {
        services.AddScoped<IDynamoRepository<T>>(provider =>
        {
            var context = provider.GetRequiredService<IDynamoDBContext>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            return new DynamoRepository<T>(context, configuration);
        });
        return services;
    }

    /// <summary>
    ///     添加所有业务仓储服务.
    /// </summary>
    /// <returns></returns>
    public static IServiceCollection AddBusinessRepositories(this IServiceCollection services)
    {
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        return services;
    }
}