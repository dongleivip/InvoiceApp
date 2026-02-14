using Amazon.DynamoDBv2.DocumentModel;
using InvoiceApi.Models;

namespace InvoiceApi.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly IDynamoRepository<Customer> _repository;

    public CustomerRepository(IDynamoRepository<Customer> repository)
    {
        _repository = repository;
    }

    public async Task<Customer?> GetByIdAsync(string customerId)
    {
        return await _repository.GetAsync($"CUST#{customerId}", "METADATA");
    }

    public async Task CreateAsync(Customer customer)
    {
        if (string.IsNullOrEmpty(customer.PartitionKey))
        {
            var customerId = Guid.NewGuid().ToString();
            customer.PartitionKey = $"CUST#{customerId}";
        }

        // 设置时间戳
        customer.UpdatedAt = DateTime.Now;

        await _repository.CreateAsync(customer);
    }

    public async Task UpdateAsync(Customer customer)
    {
        // Todo: 更新业务信息
        
        // 更新时间戳
        customer.UpdatedAt = DateTime.Now;

        // 只需保存实体，DynamoDB 上下文会处理更新
        await _repository.CreateAsync(customer);
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        // 获取所有客户 (利用 GSI1 索引)
        var config = new QueryOperationConfig
        {
            // 指定使用 GSI1 索引，而不是主表
            IndexName = "GSI1",
            // 只需要查询 GSI1PK 为固定常量的记录
            Filter = new QueryFilter("GSI1PK", QueryOperator.Equal, "CUST#ALL"),
            // --- 排序控制 ---
            // BackwardSearch = false (默认): 从旧到新 (升序) / true: 从新到旧 (降序，常用于显示最新用户)
            BackwardSearch = true
            // 注意：这里由于是查索引，不需要在 QueryAsync 内部再次 AddCondition("PK"...)
        };

        return await _repository.QueryAsync("CUST#ALL", config);
    }

    public async Task DeleteAsync(string pk, string sk)
    {
        await _repository.DeleteAsync(pk, sk);
    }
}