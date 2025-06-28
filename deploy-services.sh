#!/bin/bash

set -e

echo "🚀 Deploying Parking Spot Finder services to Azure..."

if [ ! -f "azure-config.env" ]; then
    echo "❌ azure-config.env not found. Please run './setup-azure-resources.sh' first."
    exit 1
fi

if [ ! -f "deployment-config.env" ]; then
    echo "❌ deployment-config.env not found. Please create and configure it first."
    echo "   See deployment-config.env for required settings."
    exit 1
fi

source azure-config.env
source deployment-config.env

echo "📝 Using configuration from config files"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Container Registry: $CONTAINER_REGISTRY"
echo "  PostgreSQL: $(echo $POSTGRES_CONNECTION_STRING | cut -d';' -f1)"
echo ""

if [ -z "$POSTGRES_CONNECTION_STRING" ]; then
    echo "❌ POSTGRES_CONNECTION_STRING not configured in deployment-config.env"
    exit 1
fi

if [ -z "$AZURE_SUBSCRIPTION_ID" ]; then
    echo "❌ AZURE_SUBSCRIPTION_ID not configured in deployment-config.env"
    exit 1
fi

if [ -n "$AZURE_SUBSCRIPTION_ID" ] && [ "$AZURE_SUBSCRIPTION_ID" != "your-subscription-id-here" ]; then
    SUBSCRIPTION_ID=$AZURE_SUBSCRIPTION_ID
    az account set --subscription $SUBSCRIPTION_ID
else
    SUBSCRIPTION_ID=$(az account show --query id -o tsv)
fi
echo "🔍 Using Azure Subscription: $SUBSCRIPTION_ID"

cd ParkingSpotFinder

echo "🔐 Logging into Container Registry..."
az acr login --name $CONTAINER_REGISTRY

echo "🔨 Building and pushing REST API..."
docker build -t restapi -f RestApi/Dockerfile .
docker tag restapi $CONTAINER_REGISTRY.azurecr.io/restapi:latest
docker push $CONTAINER_REGISTRY.azurecr.io/restapi:latest
echo "✅ REST API pushed"

echo "🔨 Building and pushing Camera service..."
docker build -t camera -f Camera/Dockerfile .
docker tag camera $CONTAINER_REGISTRY.azurecr.io/camera:latest
docker push $CONTAINER_REGISTRY.azurecr.io/camera:latest
echo "✅ Camera service pushed"

echo "🔨 Building and pushing AI Vision Model..."
docker build -t aivisionmodel -f AiVisionModel/Dockerfile .
docker tag aivisionmodel $CONTAINER_REGISTRY.azurecr.io/aivisionmodel:latest
docker push $CONTAINER_REGISTRY.azurecr.io/aivisionmodel:latest
echo "✅ AI Vision Model pushed"

echo "🌐 Deploying REST API to App Service..."
WEB_APP_NAME="parking-spot-finder-api-$(date +%s)"
az webapp create --resource-group $RESOURCE_GROUP --plan $APP_SERVICE_PLAN --name $WEB_APP_NAME --deployment-container-image-name $CONTAINER_REGISTRY.azurecr.io/restapi:latest

if [ -z "$CAMERA_CONTAINER_IMAGE" ]; then
    CAMERA_IMAGE="$CONTAINER_REGISTRY.azurecr.io/camera:latest"
    echo "📷 Using auto-built camera image: $CAMERA_IMAGE"
else
    CAMERA_IMAGE="$CAMERA_CONTAINER_IMAGE"
    echo "📷 Using custom camera image: $CAMERA_IMAGE"
fi

ACR_LOGIN_SERVER="$CONTAINER_REGISTRY.azurecr.io"
ACR_USERNAME="$CONTAINER_REGISTRY"
ACR_PASSWORD=$(az acr credential show --name $CONTAINER_REGISTRY --query "passwords[0].value" -o tsv)

echo "⚙️ Configuring REST API settings..."
az webapp config appsettings set --resource-group $RESOURCE_GROUP --name $WEB_APP_NAME --settings \
    "ConnectionStrings__DefaultConnection=$POSTGRES_CONNECTION_STRING" \
    "AZURE_SUBSCRIPTION_ID=$SUBSCRIPTION_ID" \
    "AZURE_RESOURCE_GROUP=$RESOURCE_GROUP" \
    "AZURE_REGION=$LOCATION" \
    "CAMERA_CONTAINER_IMAGE=$CAMERA_IMAGE" \
    "ACR_LOGIN_SERVER=$ACR_LOGIN_SERVER" \
    "ACR_USERNAME=$ACR_USERNAME" \
    "ACR_PASSWORD=$ACR_PASSWORD"

echo "✅ REST API deployed"

echo "🤖 Deploying AI Vision Model to Container Instance..."
AI_INSTANCE_NAME="ai-vision-model-$(date +%s)"

az container create \
    --resource-group $RESOURCE_GROUP \
    --name $AI_INSTANCE_NAME \
    --image $CONTAINER_REGISTRY.azurecr.io/aivisionmodel:latest \
    --registry-login-server $ACR_LOGIN_SERVER \
    --registry-username $ACR_USERNAME \
    --registry-password $ACR_PASSWORD \
    --dns-name-label $AI_INSTANCE_NAME \
    --ports 80 \
    --cpu 2 \
    --memory 4 \
    --os-type Linux

echo "✅ AI Vision Model deployed"

echo "⚡ Deploying Azure Functions..."

if ! command -v func &> /dev/null; then
    echo "❌ Azure Functions Core Tools not found. Please install it:"
    echo "   npm install -g azure-functions-core-tools@4 --unsafe-perm true"
    echo "   Or visit: https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local"
    exit 1
fi

if [ -d "ImageDownloader" ]; then
    echo "📥 Deploying ImageDownloader function..."
    cd ImageDownloader
    func azure functionapp publish $FUNCTION_APP_NAME --force
    cd ..
    echo "✅ ImageDownloader deployed"
else
    echo "⚠️ ImageDownloader directory not found, skipping..."
fi

if [ -d "ImageProcessor" ]; then
    echo "🖼️ Deploying ImageProcessor function..."
    cd ImageProcessor
    func azure functionapp publish $FUNCTION_APP_NAME --force
    cd ..
    echo "✅ ImageProcessor deployed"
else
    echo "⚠️ ImageProcessor directory not found, skipping..."
fi

echo "🔍 Getting service URLs..."
REST_API_URL=$(az webapp show --resource-group $RESOURCE_GROUP --name $WEB_APP_NAME --query "defaultHostName" -o tsv)
AI_VISION_URL=$(az container show --resource-group $RESOURCE_GROUP --name $AI_INSTANCE_NAME --query "ipAddress.fqdn" -o tsv)

echo "⚙️ Configuring Function App settings..."
az functionapp config appsettings set --name $FUNCTION_APP_NAME --resource-group $RESOURCE_GROUP --settings \
    "ConnectionStrings__DefaultConnection=$POSTGRES_CONNECTION_STRING" \
    "REST_API_URL=https://$REST_API_URL" \
    "AI_VISION_MODEL_URL=http://$AI_VISION_URL"

cat > service-urls.env << EOF
REST_API_URL=https://$REST_API_URL
AI_VISION_URL=http://$AI_VISION_URL
FUNCTION_APP_URL=https://$FUNCTION_APP_NAME.azurewebsites.net
WEB_APP_NAME=$WEB_APP_NAME
AI_INSTANCE_NAME=$AI_INSTANCE_NAME
EOF

echo ""
echo "🎉 Deployment complete!"
echo ""
echo "📋 Deployed services:"
echo "  ✅ REST API: https://$REST_API_URL"
echo "  ✅ AI Vision Model: http://$AI_VISION_URL"
echo "  ✅ Function App: https://$FUNCTION_APP_NAME.azurewebsites.net"
echo "  ✅ Camera Image: $CAMERA_IMAGE (ready for auto-deployment)"
echo ""
echo "📷 Camera Deployment:"
echo "  • Camera containers are auto-deployed when you create parking lots"
echo "  • Using your built Camera project image: $CAMERA_IMAGE"
echo "  • Each parking lot gets its own camera container instance"
echo ""
echo "📝 Service URLs saved to: service-urls.env"
echo "🧪 Next step: Run './test-deployment.sh' to test your deployment"