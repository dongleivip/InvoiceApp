# 使用 .NET SDK 镜像作为构建环境
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# 复制项目文件并还原依赖项
COPY invoice-api/*.csproj ./invoice-api/
RUN dotnet restore ./invoice-api/

# 复制其余文件并发布应用
COPY invoice-api/. ./invoice-api/
WORKDIR /app/invoice-api
RUN dotnet publish -c Release -o out

# 使用 ASP.NET 运行时镜像作为最终阶段
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build-env /app/invoice-api/out .

# 暴露端口
EXPOSE 8080

# 设置入口点
ENTRYPOINT ["dotnet", "InvoiceApi.dll"]