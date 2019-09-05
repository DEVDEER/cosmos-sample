# cosmos-sample
A sample showing high-performance write to CosmosDB and introducing an Azure Function syncing the CosmosDB entries to SQL Azure.

## Summary

The intend of this project is to demonstrate a way to use Cosmos Change Feed in order to extract certain properties of newly created CosmosDB-documents in SQL Azure.

The reason for this is that it is relatively expensive to perform aggregations on CosmosDB and therefore it might be a good idea to extract certain informations of Cosmos-Documents into another storage.

## Step-By-Step

### Prepare your Azure Subscription

You should create all resources needed in the first step. First pull the repository locally. Choose a text-editor and create a new text-file. Paste the following JSON into it:

    {
      "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#",
      "contentVersion": "1.0.0.0",
      "parameters": {
        "function_account_name": {
          "value": "??"
        },
        "sql_server_name": {
          "value": "??"
        },
        "sql_server_user": {
          "value": "??"
        },
        "sql_server_password": {
          "value": "??"
        },
        "app_service_plan_name": {
          "value": "??"
        },
        "application_insights_name": {
          "value": "??"
        },
        "storage_account_name": {
          "value": "??"
        },
        "cosmos_db_account_name": {
          "value": "??"
        }
      }
    }

Now replace all "??"-strings with appropriate values and store the file under the name `azuredeploy.parameters.json` in the folder `/Azure.ResourceGroup`.

Now open a command prompt or terminal in the folder `/Azure.ResourceGroup` and perform one of the following commands:

- Bash: `./deploy.sh`
- PowerShell: `./deploy.ps1`

You will be asked to input some variables:

- Subscription: Paste in your Azure subscription ID
- ResourceGroupName: Deliver a name for the new resource group which
- ResourceGroupLocation: Deliver a valid resource group location (e.g. westeurope)

After some time a new resource group is created under the provided subscription. It should contain the following resources:

- Application Insights
- Azure Cosmos DB account
- App Service
- SQL Server
- SQL Database
- Storage Account
- App Service Plan

### Deploy Function App

TODO

### 

## Content

This repo contains 3 projects mainly:
- Azure.ResourceGroup: Contains the ARM template including scripts to execute it.
- Serverless: Contains a VS Code project containing the Azure Function
- Ui.CreationConsole: Contains a .NET Core Console Application which can be used to add a bulk of documents to Cosmos DB
