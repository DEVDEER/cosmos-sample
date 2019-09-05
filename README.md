# cosmos-sample
A sample showing high-performance write to CosmosDB and introducing an Azure Function syncing the CosmosDB entries to SQL Azure.

## Summary

The intend of this project is to demonstrate a way to use Cosmos Change Feed in order to extract certain properties of newly created CosmosDB-documents in SQL Azure.

The reason for this is that it is relatively expensive to perform aggregations on CosmosDB and therefore it might be a good idea to extract certain informations of Cosmos-Documents into another storage.

## Step-By-Step

### Prepare your Azure Subscription

You should create all resources needed in the first step. First pull the repository locally. Choose a text-editor and open thr file `create.sh`. There are several lines of variable assignment in the first block of the file. Setup your environment by assigning the correct values there. Save and close the file.

Now open a command prompt or terminal in the folder `/Azure.ResourceGroup` and perform one of the following commands:

- Bash: `./create.sh`

**Important:** Ensure that your are logged in to your Azure Subscription and that you've selected the correct account using `az account show` first!

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
