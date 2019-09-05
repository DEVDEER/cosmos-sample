# bin/bash
# Change this values!
RESOURCEGROUP_NAME='TODO'
LOCATION='TODO'
SQL_SERVER_NAME='TODO'
SQL_ADMIN_USERNAME='TODO'
SQL_ADMIN_PASSWORD='TODO'
STORAGE_ACCOUNT_NAME='TODO'
APPINSIGHTS_NAME='TODO'
SERVICEPLAN_NAME='TODO'
SERVICEPLAN_SKU='TODO'
FUNCTIONAPP_NAME='TODO'
COSMOSDB_NAME='TODO'

# don't touch from here!
GREY='\033[0;37m'
GREEN='\033[0;32m'

echo -e "${GREY}Creating resource group $RESOURCEGROUP_NAME..."
az group create -n $RESOURCEGROUP_NAME --location $LOCATION --output none
echo -e "${GREEN}Finished."

echo -e "${GREY}Creating SQL Server $SQL_SERVER_NAME with database test..."
az sql server create -n $SQL_SERVER_NAME --location $LOCATION --resource-group $RESOURCEGROUP_NAME --admin-user $SQL_ADMIN_USERNAME --admin-password $SQL_ADMIN_PASSWORD --output none
az sql server firewall-rule create --resource-group $RESOURCEGROUP_NAME --server $SQL_SERVER_NAME -n AllowAllWindowsAzureIps --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 --output none
az sql db create --resource-group $RESOURCEGROUP_NAME --server $SQL_SERVER_NAME -n test --output none
echo -e "${GREEN}Finished."

echo -e "${GREY}Creating storage account $STORAGE_ACCOUNT_NAME..."
az storage account create --resource-group $RESOURCEGROUP_NAME -n $STORAGE_ACCOUNT_NAME --output none
echo -e "${GREEN}Finished."

echo -e "${GREY}Creating Application Insights $APPINSIGHTS_NAME..."
az resource create --resource-group $RESOURCEGROUP_NAME --resource-type "Microsoft.Insights/components" -n $APPINSIGHTS_NAME --location $LOCATION -p '{"Application_Type":"web"}' --output none
echo -e "${GREEN}Finished."

echo -e "${GREY}Creating CosmosDB $COSMOSDB_NAME..."
az cosmosdb create --resource-group $RESOURCEGROUP_NAME -n $COSMOSDB_NAME --output none
COSMOSDB_KEY=$( az cosmosdb list-keys --name $COSMOSDB_NAME --resource-group $RESOURCEGROUP_NAME --query primaryMasterKey -o tsv)
COSMOS_CONNECTION=$(az cosmosdb list-connection-strings --name $COSMOSDB_NAME --resource-group $RESOURCEGROUP_NAME --query connectionStrings[0].connectionString -o tsv)
az cosmosdb database create --name $COSMOSDB_NAME --db-name Sample --throughput 400 --key $COSMOSDB_KEY --output none
az cosmosdb collection create --resource-group $RESOURCEGROUP_NAME --collection-name orders --name $COSMOSDB_NAME --db-name Sample --partition-key-path /productId --throughput 400 --output none
echo -e "${GREEN}Finished."

echo -e "${GREY}Creating Function App $FUNCTIONAPP_NAME with App Service Plan $SERVICEPLAN_NAME..."
az appservice plan create -n $SERVICEPLAN_NAME --resource-group $RESOURCEGROUP_NAME --sku $SERVICEPLAN_SKU  --output none
az functionapp create --resource-group $RESOURCE_GROUP -n $FUNCTIONAPP_NAME --storage-account $STORAGE_ACCOUNT_NAME --runtime node --app-insights $APPINSIGHTS_NAME --output none
az functionapp config appsettings set --name $FUNCTIONAPP_NAME --resource-group $RESOURCEGROUP_NAME --settings "CosmosConnectionString="
az functionapp config appsettings set --name $FUNCTIONAPP_NAME --resource-group $RESOURCEGROUP_NAME --settings "SQL_SERVER_ADDRESS=$SQL_SERVER_NAME.database.windows.net"
az functionapp config appsettings set --name $FUNCTIONAPP_NAME --resource-group $RESOURCEGROUP_NAME --settings "SQL_SERVER_USER=$SQL_ADMIN_USERNAME"
az functionapp config appsettings set --name $FUNCTIONAPP_NAME --resource-group $RESOURCEGROUP_NAME --settings "SQL_SERVER_PASSWORD=$SQL_ADMIN_PASSWORD"
echo -e "${GREEN}Finished."