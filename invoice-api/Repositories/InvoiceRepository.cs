namespace InvoiceApi.Repositories;

using Amazon.DynamoDBv2.DocumentModel;
using Models;


public class InvoiceRepository : IInvoiceRepository
{
    private readonly IDynamoRepository<Invoice> _repository;

    public InvoiceRepository(IDynamoRepository<Invoice> repository)
    {
        _repository = repository;
    }

    public async Task<Invoice?> GetByIdAsync(string customerId, string invoiceId, string issueDate)
    {
        // Note: issueDate format is yyyy-MM-dd
        var sk = $"INV#{issueDate}#{invoiceId}";
        return await _repository.GetAsync($"CUST#{customerId}", sk);
    }

    public async Task<Invoice?> GetByOnlyIdAsync(string invoiceId)
    {
        var config = new QueryOperationConfig
        {
            IndexName = "GSI2",
            Filter = new QueryFilter("GSI2PK", QueryOperator.Equal, $"INV#{invoiceId}"),
        };

        // 虽然调用的是 Query，但因为 ID 唯一，结果集只会有一个
        var results = await _repository.QueryAsync($"INV#{invoiceId}", config);
        return results.FirstOrDefault();
    }


    public async Task<IEnumerable<Invoice>> GetByCustomerAsync(string customerId)
    {
        // 利用通用 QueryAsync 查询该分区(customerId)下的所有订单
        var config = new QueryOperationConfig
        {
            // 比如只查以 INV# 开头的排序键
            Filter = new QueryFilter("SK", QueryOperator.BeginsWith, "INV#"),
        };

        return await _repository.QueryAsync($"CUST#{customerId}", config);
    }

    /// <summary>
    /// Get Invoices for specific customer with a certain period.
    /// </summary>
    /// <param name="customerId">customer identifier.</param>
    /// <returns>All Invoices belong to the customer.</returns>
    public async Task<IEnumerable<Invoice>> GetByCustomerAndDateAsync(string customerId, DateTime start, DateTime end)
    {
        var config = new QueryOperationConfig
        {
            // \uffff 确保包含结束当天的所有 ID,
            Filter = new QueryFilter(
                "SK",
                QueryOperator.Between,
                $"INV#{start:yyyy-MM-dd}",
                $"INV#{end:yyyy-MM-dd}#\uffff"),
        };

        return await _repository.QueryAsync($"CUST#{customerId}", config);
    }

    public async Task<IEnumerable<Invoice>> GetAllByDateRangeAsync(DateTime start, DateTime end)
    {
        var config = new QueryOperationConfig
        {
            IndexName = "GSI1",
            Filter = new QueryFilter("GSI1PK", QueryOperator.Equal, "INV#ALL"),
        };

        config.Filter.AddCondition(
            "GSI1SK",
            QueryOperator.Between,
            start.ToString("yyyy-MM-dd"),
            end.ToString("yyyy-MM-ddTHH:mm:ss") + "#\uffff");

        return await _repository.QueryAsync("INV#ALL", config);
    }

    public async Task CreateAsync(Invoice invoice)
    {
        // 设置时间戳
        var now = DateTime.Now;
        invoice.CreatedAt = now;
        invoice.UpdatedAt = now;

        // GSI1，支持后续的跨客户查询和 ID 检索
        invoice.Gsi1Pk = $"CUST#{invoice.CustomerId}";
        invoice.Gsi1Sk = $"INV#{now:yy-MM-dd}#{invoice.Id}";

        // GSI2 允许在没有日期的情况下直接通过 ID 定位 Invoice
        invoice.Gsi2Pk = $"INV#{invoice.Id}";
        invoice.Gsi2Sk = "METADATA";

        await _repository.CreateAsync(invoice);
    }

    public async Task UpdateAsync(Invoice invoice)
    {
        // 更新时间戳
        invoice.UpdatedAt = DateTime.Now;

        // 注意：在 DynamoDBContext 中，SaveAsync 是 Upsert（覆盖写入）
        // 如果你只想部分更新（如仅修改状态），建议先 Get 再 Save
        await _repository.CreateAsync(invoice); // 使用 CreateAsync，DynamoDB 的 SaveAsync 会覆盖现有项
    }

    public async Task DeleteAsync(string customerId, string invoiceId, string issueDate)
    {
        // Note: issueDate is yyyy-MM-dd
        var sk = $"INV#{issueDate}#{invoiceId}";
        await _repository.DeleteAsync($"CUST#{customerId}", sk);
    }

    public async Task DeleteByKeyAsync(string pk, string sk)
    {
        await _repository.DeleteAsync(pk, sk);
    }

    public async Task DeleteByIdAsync(string invoiceId)
    {
        // 首先通过 GSI2 找到完整的 PK 和 SK
        var config = new QueryOperationConfig
        {
            IndexName = "GSI2",
            Filter = new QueryFilter("GSI2PK", QueryOperator.Equal, $"INV#{invoiceId}"),
        };

        var results = await _repository.QueryAsync($"INV#{invoiceId}", config);
        var invoice = results.FirstOrDefault();

        // 在通过 PK 和 SK 完成删除
        if (invoice != null)
        {
            await _repository.DeleteAsync(invoice.PartitionKey, invoice.SortKey);
        }
    }
}