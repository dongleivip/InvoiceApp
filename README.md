# Invoice Management API - Local Development Guide

## 概述

这个 Invoice Management API 使用 .NET 9 Minimal API 构建，部署在 AWS Lambda 上，使用 DynamoDB 单表设计作为数据库。

## 本地开发（无需 AWS CLI）

### 前置条件

- Docker 和 Docker Compose
- .NET 9 SDK（可选，用于本地开发）

### 启动服务

1. **启动 LocalStack 和 API 服务**

```bash
docker-compose up -d localstack invoice-api
```

2. **初始化 DynamoDB 表（无需 AWS CLI）**

我们提供了两种方式初始化数据库：

#### 方式一：使用 .NET 初始化工具（推荐）

```bash
docker-compose --profile init up db-initializer
```

这个命令会：
- 自动构建并运行 .NET 初始化工具
- 等待 LocalStack 准备就绪
- 创建 DynamoDB 表:
  - dev: `Dev_InvoiceApp`
  - prod: `Prod_InvoiceApp`

#### 方式二：使用脚本（需要 AWS CLI）

如果你已经安装了 AWS CLI，可以使用：

```bash
./setup-local-db.sh
```

### 验证服务

1. **检查 LocalStack 状态**

```bash
curl http://localhost:4566/_localstack/health
```

2. **测试 API 健康检查**

```bash
# 基础健康检查
curl http://localhost:5000/health

# 增强健康检查（包含数据库连接测试）
curl http://localhost:5000/healthz
```

3. **测试 API 端点**

```bash
# 获取所有客户
curl http://localhost:5000/customers

# 创建客户
curl -X POST http://localhost:5000/customers \
  -H "Content-Type: application/json" \
  -d '{"name":"Customer Name","address":"No 101, Kings St, Xyz City"}'
```

### 停止服务

```bash
docker-compose down
```

## 技术栈

- **框架**: .NET 9 Minimal API
- **部署**: AWS Lambda
- **数据库**: DynamoDB（单表设计）
- **本地模拟**: LocalStack
- **容器化**: Docker

## API 端点

### 客户管理

- `GET /customers` - 获取所有客户
- `GET /customers/{id}` - 获取特定客户
- `POST /customers` - 创建客户
- `PUT /customers/{id}` - 更新客户
- `DELETE /customers/{id}` - 删除客户

### 发票管理

- `GET /invoices` - 获取所有发票
- `GET /invoices/{id}` - 获取特定发票
- `POST /invoices` - 创建发票
- `PUT /invoices/{id}` - 更新发票
- `DELETE /invoices/{id}` - 删除发票
- `GET /customers/{customerId}/invoices` - 获取客户的所有发票

### 健康检查

- `GET /health` - 基础健康检查
- `GET /healthz` - 增强健康检查（包含数据库连接测试）

## 配置说明

### DynamoDB 单表设计

表名：`Dev_InvoiceApp`

- **主键**: `PK` (分区键) + `SK` (排序键)
- **GSI1**: `gsi_pk` + `gsi_sk`

实体类型：
- 客户(Customer): `PK=CUST#{id}`, `SK=METADATA`, `GSI1PK=CUST#ALL`, `GSI1SK={yyyy-MM-ddTHH:mm:ss}`, `EntityType=Customer`
- 发票(Invoice): `PK=CUST#{id}`, `SK=INV#{id}`

### DynomoDB Table Schame

Schame 详见 [table_schame.json](./table_schema.json)文件。

### 导出 Table Schame / 创建 Table

工具 awslocal (`brew install awscli-local)

```bash
awslocal dynamodb describe-table --table-name Dev_InvoiceApp \
  --query '{
    TableName: Table.TableName,
    AttributeDefinitions: Table.AttributeDefinitions,
    KeySchema: Table.KeySchema,
    BillingMode: Table.BillingModeSummary.BillingMode,
    GlobalSecondaryIndexes: Table.GlobalSecondaryIndexes[].{
        IndexName: IndexName,
        KeySchema: KeySchema,
        Projection: Projection
    }
  }' > table_schema.json
```

```bash
awslocal dynamodb create-table --cli-input-json file://table_schema.json
```

### 插入初始数据

```bash
awslocal dynamodb batch-write-item --request-items file://table_init_data.json
```

将表中数据导出以方便将来重建表：

```bash
awslocal dynamodb scan --table-name Dev_InvoiceApp \
  jq '{"Dev_InvoiceApp": [.Items[] | {PutRequest: {Item: .}}]}' > table_init_data.json
```

### 环境变量

- `ASPNETCORE_ENVIRONMENT`: 运行环境（Development/Production）
- `AWS_ENDPOINT_URL`: AWS 服务端点（本地开发使用 `http://localhost:4566`）

## 故障排除

### LocalStack 无法启动

检查端口是否被占用：
```bash
lsof -i :4566
```

### DynamoDB 连接失败

确保 LocalStack 完全启动：
```bash
docker logs localstack_main
```

### 表创建失败

检查初始化日志：
```bash
docker logs invoice_db_initializer
```

## 生产部署

生产环境部署到 AWS Lambda 时：

1. 移除 `AWS_ENDPOINT_URL` 环境变量
2. 确保 IAM 角色有 DynamoDB 访问权限
3. 配置合适的 Lambda 内存和超时设置
4. 使用 AWS Systems Manager Parameter Store 管理配置
