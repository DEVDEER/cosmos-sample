# cosmos-sample
A sample showing high-performance write to CosmosDB and introducing an Azure Function syncing the CosmosDB entries to SQL Azure.

## Summary

The intend of this project is to demonstrate a way to use Cosmos Change Feed in order to extract certain properties of newly created CosmosDB-documents in SQL Azure.

The reason for this is that it is relatively expensive to perform aggregations on CosmosDB and therefore it might be a good idea to extract certain informations of Cosmos-Documents into another storage.

## Content

This repo contains 3 projects mainly:
- Azure.ResourceGroup: Contains the ARM template including scripts to execute it.
- Serverless: Contains a VS Code project containing the Azure Function
- Ui.CreationConsole: Contains a .NET Core Console Application which can be used to add a bulk of documents to Cosmos DB
