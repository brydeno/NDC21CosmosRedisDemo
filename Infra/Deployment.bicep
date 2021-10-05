@maxLength(30) 
param applicationName string = 'lockingdemo-${uniqueString(resourceGroup().id)}'

param location string = resourceGroup().location

param databaseName string = 'LockingDemo'
param containerName string = 'Items'

var storageAccountName = 'stor54364'

// All the various AD role id's we'll be using
var contributorRoleId = 'b24988ac-6180-42a0-ab88-20f7382dd24c'

var cosmosAccountName = toLower(applicationName)
var redisName = toLower(applicationName)
var websiteName = applicationName // why not just use the param directly?
var hostingPlanName = applicationName // why not just use the param directly?

resource redis 'Microsoft.Cache/Redis@2020-12-01' = {
  name: redisName
  location: location
  properties:{
    sku: {
      capacity: 0
      family: 'C'
      name: 'Basic'
    }
  }
}

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2020-04-01' = {
  name: cosmosAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    databaseAccountOfferType: 'Standard'
    // this means you must use RBAC
    disableKeyBasedMetadataWriteAccess: true
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2021-03-15' = {
  name: '${cosmos.name}/demo'
  properties: {
    resource: {
      id: 'demo'
    }
  }
}
resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2021-03-15' = {
  name: '${database.name}/democontainer'
  properties:{
    resource: {
      id: 'democontainer'
      partitionKey: {
        paths: [
          '/partitionKey'
        ]
      }
    }
  }
}
 

resource farm 'Microsoft.Web/serverFarms@2020-06-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

resource appinsights 'Microsoft.Insights/components@2020-02-02-preview' ={
  name: '${websiteName}-ai'
  location: location
  kind: 'web'
  properties: {
    'Application_Type': 'web'   
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2019-06-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
}

resource website 'Microsoft.Web/sites@2020-06-01' = {
  name: websiteName
  location: location
  identity:{
    type: 'SystemAssigned'
  }
  kind: 'functionapp'
  properties: {
    serverFarmId: farm.id
    httpsOnly: true
    siteConfig: {
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appinsights.properties.InstrumentationKey
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value}'
        }
        {
          'name': 'FUNCTIONS_EXTENSION_VERSION'
          'value': '~3'
        }
        {
          'name': 'FUNCTIONS_WORKER_RUNTIME'
          'value': 'dotnet'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value}'
        }
        {
          name: 'CosmosDb:Endpoint'
          value: cosmos.properties.documentEndpoint
        }
        {
          name: 'CosmosDb:AuthKey'
          value: cosmos.listKeys().primaryMasterKey
        }
        {
          name: 'CosmosDb:DatabaseName'
          value: databaseName
        }
        {
          name: 'CosmosDb:ContainerName'
          value: containerName
        }
        {
          name: 'Redis:connectString'
          value: '${redis.name}.redis.cache.windows.net:6380,password=${redis.listKeys().primaryMasterKey}'          
        }
      ]
    }
  }
}

// Give the managed identity contributor role to Cosmos
resource cosmosRole 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: guid(contributorRoleId, cosmos.id)
  scope: cosmos
  properties: {
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', contributorRoleId)
    principalId: website.identity.principalId
  }
}
