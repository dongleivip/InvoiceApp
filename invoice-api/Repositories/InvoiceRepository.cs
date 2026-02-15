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

    public async Task<Invoice?> GetByIdAsync(string invoiceId)
    {
        return await _repository.GetAsync($"INV#{invoiceId}", "METADATA");
    }

    public async Task<IEnumerable<Invoice>> GetCustomerInvoicesAsync(string customerId)
    {
        // 利用通用 QueryAsync 查询该分区(customerId)下的所有订单
        var config = new QueryOperationConfig
        {
            // 比如只查以 INV# 开头的排序键
            Filter = new QueryFilter("SK", QueryOperator.BeginsWith, "INV#"),
        };

        return await _repository.QueryAsync($"CUST#{customerId}", config);
    }

    public async Task CreateAsync(Invoice invoice)
    {
        if (string.IsNullOrEmpty(invoice.Id))
        {
            invoice.Id = Guid.NewGuid().ToString();
        }

        // 设置时间戳
        var now = DateTime.Now;
        invoice.CreatedAt = now;
        invoice.UpdatedAt = now;

        // 设置 GSI 键值
        invoice.Gsi1Pk = $"INV#{invoice.CustomerId}"; // 按客户 ID 分组
        invoice.Gsi1Sk = now.ToString("yyyy-MM-ddTHH:mm:ss"); // 按日期排序

        await _repository.CreateAsync(invoice);
    }

    public async Task UpdateAsync(Invoice invoice)
    {
        // 更新时间戳
        invoice.UpdatedAt = DateTime.Now;

        // 只需保存实体，DynamoDB 上下文会处理更新
        await _repository.CreateAsync(invoice); // 使用 CreateAsync，DynamoDB 的 SaveAsync 会覆盖现有项
    }

    public async Task DeleteAsync(string pk, string sk)
    {
        await _repository.DeleteAsync(pk, sk);
    }
}