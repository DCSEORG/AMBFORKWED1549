targetScope = 'resourceGroup'

@description('Location for all resources')
param location string = 'uksouth'

@description('Unique suffix for resource names')
param uniqueSuffix string = uniqueString(resourceGroup().id)

@description('Azure AD Admin Object ID for SQL Server')
param adminObjectId string

@description('Azure AD Admin Login for SQL Server')
param adminLogin string

@description('Deploy GenAI resources')
param deployGenAI bool = false

// Deploy App Service
module appService 'app-service.bicep' = {
  name: 'app-service-deployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
  }
}

// Deploy Azure SQL
module azureSQL 'azure-sql.bicep' = {
  name: 'azure-sql-deployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Deploy GenAI resources (conditional)
module genai 'genai.bicep' = if (deployGenAI) {
  name: 'genai-deployment'
  params: {
    location: location
    uniqueSuffix: uniqueSuffix
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = appService.outputs.managedIdentityPrincipalId
output sqlServerName string = azureSQL.outputs.sqlServerName
output sqlDatabaseName string = azureSQL.outputs.sqlDatabaseName
output sqlServerFqdn string = azureSQL.outputs.sqlServerFqdn
output openAIEndpoint string = deployGenAI ? genai.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genai.outputs.openAIModelName : ''
output openAIName string = deployGenAI ? genai.outputs.openAIName : ''
output searchEndpoint string = deployGenAI ? genai.outputs.searchEndpoint : ''
output searchName string = deployGenAI ? genai.outputs.searchName : ''
