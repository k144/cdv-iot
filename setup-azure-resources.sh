#!/bin/bash


set -e

echo "🚀 Setting up Azure resources for Parking Spot Finder..."

RESOURCE_GROUP="parking-spot-finder-rg"
LOCATION="eastus"
CONTAINER_REGISTRY="parkingspotfinderacr$(date +%s)"
APP_SERVICE_PLAN="parking-spot-finder-plan"
FUNCTION_APP_NAME="parking-spot-finder-functions-$(date +%s)"
STORAGE_ACCOUNT="parkingstorage$(date +%s)"

echo "📝 Configuration:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo "  Container Registry: $CONTAINER_REGISTRY"
echo "  App Service Plan: $APP_SERVICE_PLAN"
echo "  Function App: $FUNCTION_APP_NAME"
echo "  Storage Account: $STORAGE_ACCOUNT"
echo ""

echo "🔐 Checking Azure login..."
if ! az account show > /dev/null 2>&1; then
    echo "❌ Not logged in to Azure. Please run 'az login' first."
    exit 1
fi

echo "✅ Azure login confirmed"

echo "📦 Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION
echo "✅ Resource group created"

echo "🐳 Creating Azure Container Registry..."
az acr create --resource-group $RESOURCE_GROUP --name $CONTAINER_REGISTRY --sku Basic --admin-enabled true
echo "✅ Container Registry created"

echo "🌐 Creating App Service Plan..."
az appservice plan create --name $APP_SERVICE_PLAN --resource-group $RESOURCE_GROUP --sku B1 --is-linux
echo "✅ App Service Plan created"

echo "💾 Creating Storage Account..."
az storage account create --name $STORAGE_ACCOUNT --resource-group $RESOURCE_GROUP --location $LOCATION --sku Standard_LRS
echo "✅ Storage Account created"

echo "⚡ Creating Function App..."
az functionapp create --resource-group $RESOURCE_GROUP --consumption-plan-location $LOCATION --runtime dotnet-isolated --functions-version 4 --name $FUNCTION_APP_NAME --storage-account $STORAGE_ACCOUNT
echo "✅ Function App created"

echo "💾 Saving configuration..."
cat > azure-config.env << EOF
RESOURCE_GROUP=$RESOURCE_GROUP
LOCATION=$LOCATION
CONTAINER_REGISTRY=$CONTAINER_REGISTRY
APP_SERVICE_PLAN=$APP_SERVICE_PLAN
FUNCTION_APP_NAME=$FUNCTION_APP_NAME
STORAGE_ACCOUNT=$STORAGE_ACCOUNT
EOF

echo ""
echo "🎉 Azure resources setup complete!"
echo ""
echo "📋 Created resources:"
echo "  ✅ Resource Group: $RESOURCE_GROUP"
echo "  ✅ Container Registry: $CONTAINER_REGISTRY"
echo "  ✅ App Service Plan: $APP_SERVICE_PLAN"
echo "  ✅ Function App: $FUNCTION_APP_NAME"
echo "  ✅ Storage Account: $STORAGE_ACCOUNT"
echo ""
echo "📝 Configuration saved to: azure-config.env"
echo "🚀 Next step: Run './deploy-services.sh' to deploy your applications"