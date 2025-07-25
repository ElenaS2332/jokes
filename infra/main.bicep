param location string = resourceGroup().location
param appServicePlanName string = ''
param webAppName string = ''
param skuName string = 'F1'

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: skuName
    tier: 'Free'
  }
  kind: 'windows'
}

resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: webAppName
  location: location
  kind: 'app,windows'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
  }
}

output endpoint string = 'https://${webApp.properties.defaultHostName}/'
