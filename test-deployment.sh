#!/bin/bash


set -e

echo "🧪 Testing Parking Spot Finder deployment..."

if [ ! -f "service-urls.env" ]; then
    echo "❌ service-urls.env not found. Please run './deploy-services.sh' first."
    exit 1
fi

source service-urls.env

echo "📝 Testing services:"
echo "  REST API: $REST_API_URL"
echo "  AI Vision Model: $AI_VISION_URL"
echo "  Function App: $FUNCTION_APP_URL"
echo ""

echo "🔍 Testing REST API health..."
if curl -s -f "$REST_API_URL/api/parkinglots/health" > /dev/null; then
    echo "✅ REST API health check passed"
    curl -s "$REST_API_URL/api/parkinglots/health" | jq .
else
    echo "❌ REST API health check failed"
    echo "   URL: $REST_API_URL/api/parkinglots/health"
fi

echo ""

echo "🤖 Testing AI Vision Model health..."
if curl -s -f "$AI_VISION_URL/health" > /dev/null; then
    echo "✅ AI Vision Model health check passed"
else
    echo "❌ AI Vision Model health check failed"
    echo "   URL: $AI_VISION_URL/health"
    echo "   Note: Container may still be starting up. Try again in a few minutes."
fi

echo ""

echo "🅿️ Testing parking lot creation (auto-deploys camera)..."
PARKING_LOT_RESPONSE=$(curl -s -X POST "$REST_API_URL/api/parkinglots" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Parking Lot",
    "location": "123 Test Street, Test City",
    "totalParkingSpaces": 50
  }')

if echo "$PARKING_LOT_RESPONSE" | jq . > /dev/null 2>&1; then
    echo "✅ Parking lot creation test passed"
    PARKING_LOT_ID=$(echo "$PARKING_LOT_RESPONSE" | jq -r '.id')
    CAMERA_URL=$(echo "$PARKING_LOT_RESPONSE" | jq -r '.cameraUrl')
    echo "   Created Parking Lot ID: $PARKING_LOT_ID"
    echo "   Camera URL: $CAMERA_URL"
    
    echo ""
    echo "🔍 Testing parking lot retrieval..."
    if curl -s -f "$REST_API_URL/api/parkinglots/$PARKING_LOT_ID" > /dev/null; then
        echo "✅ Parking lot retrieval test passed"
    else
        echo "❌ Parking lot retrieval test failed"
    fi
else
    echo "❌ Parking lot creation test failed"
    echo "Response: $PARKING_LOT_RESPONSE"
fi

echo ""

echo "📷 Testing camera configuration..."
CAMERA_CONFIG_RESPONSE=$(curl -s -X POST "$REST_API_URL/api/parkinglots/configure-camera" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Demo Camera Setup",
    "location": "456 Demo Avenue, Demo City", 
    "totalParkingSpaces": 25,
    "simulationConfig": {
      "baseOccupancyRate": 0.4,
      "trafficVariation": 0.3,
      "enableDailyPattern": true
    }
  }')

if echo "$CAMERA_CONFIG_RESPONSE" | jq . > /dev/null 2>&1; then
    echo "✅ Camera configuration test passed"
    DEMO_PARKING_LOT_ID=$(echo "$CAMERA_CONFIG_RESPONSE" | jq -r '.id')
    DEMO_CAMERA_URL=$(echo "$CAMERA_CONFIG_RESPONSE" | jq -r '.cameraUrl')
    echo "   Created Demo Parking Lot ID: $DEMO_PARKING_LOT_ID"
    echo "   Demo Camera URL: $DEMO_CAMERA_URL"
else
    echo "❌ Camera configuration test failed"
    echo "Response: $CAMERA_CONFIG_RESPONSE"
fi

echo ""

echo "📋 Testing parking lots listing..."
if curl -s -f "$REST_API_URL/api/parkinglots" > /dev/null; then
    echo "✅ Parking lots listing test passed"
    PARKING_LOTS_COUNT=$(curl -s "$REST_API_URL/api/parkinglots" | jq '. | length')
    echo "   Total parking lots: $PARKING_LOTS_COUNT"
else
    echo "❌ Parking lots listing test failed"
fi

echo ""

echo "⚡ Testing Function App..."
if curl -s -f "$FUNCTION_APP_URL" > /dev/null; then
    echo "✅ Function App is accessible"
else
    echo "❌ Function App test failed or not accessible"
    echo "   URL: $FUNCTION_APP_URL"
fi

echo ""
echo "🏁 Testing complete!"
echo ""
echo "📊 Test Summary:"
echo "   🌐 REST API: Available at $REST_API_URL"
echo "   🤖 AI Vision Model: Available at $AI_VISION_URL" 
echo "   ⚡ Function App: Available at $FUNCTION_APP_URL"
echo ""
echo "🎯 What you can do now:"
echo "   • Visit $REST_API_URL/swagger to explore the API"
echo "   • Use the API to create parking lots (automatically deploys cameras)"
echo "   • Monitor deployments in Azure Portal"
echo "   • Check Azure Container Instances for auto-deployed cameras"
echo ""
echo "🛠️ Troubleshooting:"
echo "   • If services fail, check Azure Portal logs"
echo "   • Container instances may take 2-3 minutes to start"
echo "   • Function apps may take 5-10 minutes for cold start"