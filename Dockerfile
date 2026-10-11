# 在线商店系统 —— Render 云托管镜像
# 多阶段构建：先用 SDK 编译，再用 aspnet 运行时精简镜像

# ---- 构建阶段 ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 先复制项目文件并还原依赖（利用 Docker 层缓存，源码未变时不重复还原）
COPY ["OnlineStore/OnlineStore.csproj", "OnlineStore/"]
RUN dotnet restore "OnlineStore/OnlineStore.csproj"

# 再复制全部源码并发布
COPY . .
WORKDIR /src/OnlineStore
RUN dotnet publish "OnlineStore.csproj" -c Release -o /app/publish --no-restore

# ---- 运行阶段 ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# 演示环境以 root 运行，确保 SQLite 数据库文件与 wwwroot 图片上传目录可写
USER root

# 生产环境：由 Render 在边缘终止 TLS；应用自身不再做 HTTPS 重定向（见 Program.cs）
ENV ASPNETCORE_ENVIRONMENT=Production

# Render 会在运行时注入 PORT 环境变量，用它绑定监听地址
CMD ASPNETCORE_URLS=http://0.0.0.0:$PORT dotnet OnlineStore.dll
