#!/usr/bin/env bash
set -euo pipefail

SUBSCRIPTION_NAME="${AZURE_SUBSCRIPTION:-Azure for Students}"
RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-rg-online-store-prod}"
LOCATION="${AZURE_LOCATION:-uksouth}"
PLAN_NAME="${AZURE_APP_PLAN:-asp-onlinestore-uksouth}"

az account set --subscription "$SUBSCRIPTION_NAME"

SUFFIX="$(openssl rand -hex 3)"
WEBAPP_NAME="onlinestore-${SUFFIX}"
SQL_SERVER_NAME="onlinestore-sql-${SUFFIX}"
SQL_ADMIN_USER="onlinestoreadmin"
SQL_ADMIN_PASSWORD="$(openssl rand -hex 16)Aa1"
WORK_DIR="/tmp/onlinestore-${SUFFIX}"

echo "Subscription: $SUBSCRIPTION_NAME"
echo "Resource group: $RESOURCE_GROUP"
echo "Region: $LOCATION"
echo "Web app: $WEBAPP_NAME"
echo "SQL server: $SQL_SERVER_NAME"

echo "Creating resource group..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  -o table

echo "Creating Azure SQL server..."
az sql server create \
  --name "$SQL_SERVER_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --admin-user "$SQL_ADMIN_USER" \
  --admin-password "$SQL_ADMIN_PASSWORD" \
  -o table

echo "Creating OnlineStore database..."
az sql db create \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER_NAME" \
  --name OnlineStore \
  --service-objective Basic \
  -o table

echo "Allowing Azure services to access Azure SQL..."
az sql server firewall-rule create \
  --resource-group "$RESOURCE_GROUP" \
  --server "$SQL_SERVER_NAME" \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0 \
  -o table

echo "Creating Linux App Service plan..."
az appservice plan create \
  --name "$PLAN_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --is-linux \
  --sku B1 \
  -o table

echo "Creating App Service..."
az webapp create \
  --resource-group "$RESOURCE_GROUP" \
  --plan "$PLAN_NAME" \
  --name "$WEBAPP_NAME" \
  --runtime "DOTNETCORE|8.0" \
  -o table

az webapp config set \
  --resource-group "$RESOURCE_GROUP" \
  --name "$WEBAPP_NAME" \
  --always-on true \
  --generic-configurations '{"healthCheckPath":"/health"}' \
  -o none

echo "Configuring application settings..."
az webapp config appsettings set \
  --resource-group "$RESOURCE_GROUP" \
  --name "$WEBAPP_NAME" \
  --settings \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "ASPNETCORE_URLS=http://0.0.0.0:8080" \
    "ConnectionStrings__DefaultConnection=Server=tcp:${SQL_SERVER_NAME}.database.windows.net,1433;Initial Catalog=OnlineStore;Persist Security Info=False;User ID=${SQL_ADMIN_USER};Password=${SQL_ADMIN_PASSWORD};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
  -o none

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
echo "Building application from $SCRIPT_DIR..."
dotnet publish "$SCRIPT_DIR/OnlineStore/OnlineStore.csproj" \
  -c Release \
  -o "$WORK_DIR/publish"

(
  cd "$WORK_DIR/publish"
  zip -qr "$WORK_DIR/online-store.zip" .
)

echo "Deploying application..."
az webapp deploy \
  --resource-group "$RESOURCE_GROUP" \
  --name "$WEBAPP_NAME" \
  --src-path "$WORK_DIR/online-store.zip" \
  --type zip \
  -o table

WEBAPP_URL="https://$(az webapp show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$WEBAPP_NAME" \
  --query defaultHostName \
  -o tsv)"

echo
echo "Deployment complete."
echo "Website: $WEBAPP_URL"
echo "Health check: ${WEBAPP_URL}/health"
echo "SQL server: ${SQL_SERVER_NAME}.database.windows.net"
