#!/bin/bash
set -e

echo "=========================================="
echo "Expense Management System - Deployment"
echo "=========================================="
echo ""

# Configuration
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"

# Get current user information for SQL admin
CURRENT_USER=$(az account show --query user.name -o tsv)
ADMIN_OBJECT_ID=$(az ad signed-in-user show --query id -o tsv)

echo "Deployment Configuration:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo "  Admin User: $CURRENT_USER"
echo "  Admin Object ID: $ADMIN_OBJECT_ID"
echo ""

# Step 1: Create resource group
echo "Step 1: Creating resource group..."
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION \
  --output none

echo "✓ Resource group created"
echo ""

# Step 2: Deploy infrastructure (without GenAI)
echo "Step 2: Deploying infrastructure..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infrastructure/main.bicep \
  --parameters \
    adminObjectId=$ADMIN_OBJECT_ID \
    adminLogin=$CURRENT_USER \
    deployGenAI=false \
  --query 'properties.outputs' \
  --output json)

echo "✓ Infrastructure deployed"
echo ""

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
SQL_SERVER_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerName.value')
SQL_DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlDatabaseName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
SQL_SERVER_IDENTITY_PRINCIPAL_ID=$(az sql server show \
  --resource-group $RESOURCE_GROUP \
  --name $SQL_SERVER_NAME \
  --query identity.principalId \
  --output tsv)

echo "Deployment Outputs:"
echo "  App Service: $APP_SERVICE_NAME"
echo "  SQL Server: $SQL_SERVER_NAME"
echo "  SQL Database: $SQL_DATABASE_NAME"
echo "  Managed Identity Client ID: $MANAGED_IDENTITY_CLIENT_ID"
echo ""

# Step 2b: Grant Directory Reader role to SQL Server identity
echo "Step 2b: Granting Directory Reader role to SQL Server identity..."
DIRECTORY_READER_ROLE_ID=$(az rest --method GET --uri "https://graph.microsoft.com/v1.0/directoryRoles?\$filter=roleTemplateId eq '88d8e3e3-8f55-4a1e-953a-9b9898b8876b'" --headers "Content-Type=application/json" --query "value[0].id" -o tsv 2>/dev/null || true)

if [ -z "$DIRECTORY_READER_ROLE_ID" ]; then
    echo "  Activating Directory Reader role..."
    DIRECTORY_READER_ROLE_ID=$(az rest --method POST --uri "https://graph.microsoft.com/v1.0/directoryRoles" --headers "Content-Type=application/json" --body "{\"roleTemplateId\": \"88d8e3e3-8f55-4a1e-953a-9b9898b8876b\"}" --query "id" -o tsv)
fi

echo "  Assigning Directory Reader role to SQL Server identity..."
az rest --method POST \
    --uri "https://graph.microsoft.com/v1.0/directoryRoles/${DIRECTORY_READER_ROLE_ID}/members/\$ref" \
    --headers "Content-Type=application/json" \
    --body "{\"@odata.id\": \"https://graph.microsoft.com/v1.0/directoryObjects/${SQL_SERVER_IDENTITY_PRINCIPAL_ID}\"}" 2>/dev/null || echo "  Role may already be assigned"

echo "✓ Directory Reader role granted to SQL Server identity"
echo ""

# Step 3: Configure App Service settings
echo "Step 3: Configuring App Service settings..."
CONNECTION_STRING="Server=tcp:${SQL_SERVER_FQDN},1433;Database=${SQL_DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config appsettings set \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "ConnectionStrings__DefaultConnection=$CONNECTION_STRING" \
    "AZURE_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
  --output none

echo "✓ App Service settings configured"
echo ""

# Step 4: Wait for SQL Server to be fully ready
echo "Step 4: Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30
echo "✓ Wait complete"
echo ""

# Step 5: Add current user's IP to firewall
echo "Step 5: Adding current IP to SQL Server firewall..."
MY_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name "AllowDeploymentClient" \
  --start-ip-address $MY_IP \
  --end-ip-address $MY_IP \
  --output none

echo "✓ Firewall rule added for IP: $MY_IP"
echo ""

# Step 6: Install Python dependencies
echo "Step 6: Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity
echo "✓ Python dependencies installed"
echo ""

# Step 7: Update Python scripts with actual server name
echo "Step 7: Updating Python scripts with server name..."
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/$SQL_SERVER_FQDN/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-dbrole.py && rm -f run-sql-dbrole.py.bak
sed -i.bak "s/sql-expensemgmt-placeholder.database.windows.net/$SQL_SERVER_FQDN/g" run-sql-stored-procs.py && rm -f run-sql-stored-procs.py.bak
echo "✓ Python scripts updated"
echo ""

# Step 8: Import database schema
echo "Step 8: Importing database schema..."
python3 run-sql.py
echo "✓ Database schema imported"
echo ""

# Step 9: Update script.sql with managed identity name and configure database roles
echo "Step 9: Configuring database roles for managed identity..."
# Get the managed identity name from deployment outputs
MANAGED_IDENTITY_NAME=$(az identity list --resource-group $RESOURCE_GROUP --query "[?contains(name, 'expensemgmt')].name" -o tsv | head -1)
if [ -z "$MANAGED_IDENTITY_NAME" ]; then
    echo "⚠️ Warning: Could not find managed identity name, using fallback method"
    MANAGED_IDENTITY_NAME=$(echo $APP_SERVICE_NAME | sed 's/app-/mid-/')
fi
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak
python3 run-sql-dbrole.py
echo "✓ Database roles configured"
echo ""

# Step 10: Deploy stored procedures
echo "Step 10: Deploying stored procedures..."
python3 run-sql-stored-procs.py
echo "✓ Stored procedures deployed"
echo ""

# Step 11: Build and package the application
echo "Step 11: Building and packaging the application..."
cd src/ExpenseManagement
dotnet publish -c Release -o ../../publish
cd ../../publish
zip -r ../app.zip . > /dev/null
cd ..
echo "✓ Application packaged"
echo ""

# Step 12: Deploy application code
echo "Step 12: Deploying application code to App Service..."
az webapp deploy \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --src-path ./app.zip \
  --type zip \
  --output none

echo "✓ Application code deployed"
echo ""

echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
echo ""
echo "Important Information:"
echo "  App Service URL: https://${APP_SERVICE_NAME}.azurewebsites.net/Index"
echo "  (Note: Navigate to /Index, not just the root URL)"
echo ""
echo "To run the application locally:"
echo "  1. Run 'az login' to authenticate"
echo "  2. Update appsettings.json connection string to use:"
echo "     Authentication=Active Directory Default"
echo "  3. Run 'dotnet run'"
echo ""
