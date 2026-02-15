namespace InvoiceApi.Common;

using Amazon.DynamoDBv2;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Reflection;
using Amazon.DynamoDBv2.DataModel;
using Models;

public class DynamoDbHealthCheck: IHealthCheck
{
    private readonly IAmazonDynamoDB _client;
    private readonly string _tableName;

    public DynamoDbHealthCheck(IAmazonDynamoDB client, IConfiguration config)
    {
        _client = client;

        // 动态解析：反射 DataEntity 的特性名 + 配置前缀
        var logicName = typeof(DataEntity).GetCustomAttribute<DynamoDBTableAttribute>()?.TableName;
        var prefix = config["DynamoDb:TableNamePrefix"];
        _tableName = $"{prefix}{logicName}";
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.DescribeTableAsync(_tableName, cancellationToken);
            var status = response.Table.TableStatus;

            return status == TableStatus.ACTIVE
                ? HealthCheckResult.Healthy($"Table {_tableName} is ACTIVE.")
                : HealthCheckResult.Degraded($"Table {_tableName} is in status: {status}");
        }
        catch (Exception ex)
        {
            // 如果表不存在或网络不通，返回 Unhealthy
            return HealthCheckResult.Unhealthy($"DynamoDB connection failed for table {_tableName}", ex);
        }
    }
}