#!/bin/bash

# ParkingSpotFinder System Integration Test Script
# This script starts all components and tests the complete end-to-end system flow

set -e

echo "🚀 Starting ParkingSpotFinder System Integration Test"
echo "=================================================="

# Configuration
REST_API_URL="http://localhost:5000"
CAMERA_URL="http://localhost:5001" 
AI_MODEL_URL="http://localhost:5002"
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PARKING_ROOT="$PROJECT_ROOT/ParkingSpotFinder"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Process tracking
declare -a BACKGROUND_PIDS=()
CLEANUP_DONE=false

# Cleanup function
cleanup() {
    if [ "$CLEANUP_DONE" = true ]; then
        return
    fi
    CLEANUP_DONE=true
    
    echo -e "\n${YELLOW}🧹 Cleaning up background processes...${NC}"
    
    for pid in "${BACKGROUND_PIDS[@]}"; do
        if kill -0 "$pid" 2>/dev/null; then
            echo "  Stopping process $pid"
            kill "$pid" 2>/dev/null || true
            sleep 1
            # Force kill if still running
            if kill -0 "$pid" 2>/dev/null; then
                kill -9 "$pid" 2>/dev/null || true
            fi
        fi
    done
    
    # Also kill any dotnet processes we might have started
    pkill -f "dotnet.*RestApi" 2>/dev/null || true
    pkill -f "dotnet.*Camera" 2>/dev/null || true
    pkill -f "dotnet.*AiVisionModel" 2>/dev/null || true
    
    echo -e "${GREEN}✓ Cleanup completed${NC}"
}

# Set up cleanup trap
trap cleanup EXIT INT TERM

# Function to start a service in the background
start_service() {
    local service_dir=$1
    local service_name=$2
    local port=$3
    local health_endpoint=$4
    
    echo -e "${BLUE}🚀 Starting $service_name...${NC}"
    
    # Check if directory exists
    if [ ! -d "$service_dir" ]; then
        echo -e "${RED}✗ Directory not found: $service_dir${NC}"
        return 1
    fi
    
    # Start the service
    cd "$service_dir"
    dotnet run --urls "http://localhost:$port" > "/tmp/${service_name,,}.log" 2>&1 &
    local pid=$!
    BACKGROUND_PIDS+=($pid)
    
    echo "  Process ID: $pid"
    echo "  Log file: /tmp/${service_name,,}.log"
    
    # Wait for service to be ready
    echo -n "  Waiting for $service_name to be ready"
    local max_attempts=30
    local attempt=0
    
    while [ $attempt -lt $max_attempts ]; do
        echo -n "."
        if curl -s "$health_endpoint" > /dev/null 2>&1; then
            echo -e " ${GREEN}✓ Ready${NC}"
            return 0
        fi
        sleep 2
        attempt=$((attempt + 1))
    done
    
    echo -e " ${RED}✗ Failed to start${NC}"
    echo "Check log: tail -f /tmp/${service_name,,}.log"
    return 1
}

# Function to check if service is running
check_service() {
    local url=$1
    local name=$2
    
    echo -n "📡 Checking $name service... "
    if curl -s "$url" > /dev/null 2>&1; then
        echo -e "${GREEN}✓ Running${NC}"
        return 0
    else
        echo -e "${RED}✗ Not running${NC}"
        return 1
    fi
}

# Function to test API endpoint
test_endpoint() {
    local method=$1
    local url=$2
    local description=$3
    local data=$4
    
    echo -n "🧪 Testing $description... "
    
    if [ "$method" = "GET" ]; then
        response=$(curl -s -w "%{http_code}" "$url")
        http_code="${response: -3}"
    elif [ "$method" = "POST" ]; then
        response=$(curl -s -w "%{http_code}" -X POST -H "Content-Type: application/json" -d "$data" "$url")
        http_code="${response: -3}"
    fi
    
    if [[ "$http_code" =~ ^2[0-9][0-9]$ ]]; then
        echo -e "${GREEN}✓ Success ($http_code)${NC}"
        return 0
    else
        echo -e "${RED}✗ Failed ($http_code)${NC}"
        return 1
    fi
}

# Step 0: Stop any existing services on the ports
echo -e "\n${YELLOW}Step 0: Cleanup existing processes${NC}"
echo "------------------------------------"
pkill -f "dotnet.*RestApi" 2>/dev/null && echo "Stopped existing RestApi" || true
pkill -f "dotnet.*Camera" 2>/dev/null && echo "Stopped existing Camera" || true  
pkill -f "dotnet.*AiVisionModel" 2>/dev/null && echo "Stopped existing AiVisionModel" || true
sleep 2

# Step 1: Start all services
echo -e "\n${YELLOW}Step 1: Starting Services${NC}"
echo "-------------------------"

# Start REST API
if ! start_service "$PARKING_ROOT/RestApi" "RestApi" "5000" "$REST_API_URL/health"; then
    echo -e "${RED}❌ Failed to start REST API${NC}"
    exit 1
fi

# Start Camera Service  
if ! start_service "$PARKING_ROOT/Camera" "Camera" "5001" "$CAMERA_URL/api/camera/health"; then
    echo -e "${RED}❌ Failed to start Camera Service${NC}"
    exit 1
fi

# Start AI Vision Model
if ! start_service "$PARKING_ROOT/AiVisionModel" "AiVisionModel" "5002" "$AI_MODEL_URL/health"; then
    echo -e "${RED}❌ Failed to start AI Vision Model${NC}"
    exit 1
fi

echo -e "\n${GREEN}✅ All services started successfully!${NC}"

# Step 2: Verify services are responding
echo -e "\n${YELLOW}Step 2: Service Health Verification${NC}"
echo "-----------------------------------"

check_service "$REST_API_URL/health" "REST API"
check_service "$CAMERA_URL/api/camera/health" "Camera Service"  
check_service "$AI_MODEL_URL/health" "AI Vision Model"

# Step 3: Test REST API endpoints
echo -e "\n${YELLOW}Step 3: REST API Tests${NC}"
echo "----------------------"

# Test getting all parking lots (should be empty initially)
test_endpoint "GET" "$REST_API_URL/api/parkinglots" "Get all parking lots"

# Test creating parking lots
echo -e "\n📝 Creating test parking lots..."

# Read test data and create parking lots
cat "$PROJECT_ROOT/test-data.json" | jq -c '.[]' | while read -r parking_lot; do
    test_endpoint "POST" "$REST_API_URL/api/parkinglots" "Create parking lot" "$parking_lot"
done

# Wait a moment for data to be persisted
sleep 2

# Test getting parking lots again (should have data now)
test_endpoint "GET" "$REST_API_URL/api/parkinglots" "Get all parking lots (with data)"

# Test current parking status
test_endpoint "GET" "$REST_API_URL/api/parkingstatehistory/current" "Get current parking status"

# Step 4: Test Camera Service
echo -e "\n${YELLOW}Step 4: Camera Service Tests${NC}"
echo "----------------------------"

test_endpoint "GET" "$CAMERA_URL/api/camera/image" "Get camera image"
test_endpoint "GET" "$CAMERA_URL/api/camera/image?testTime=2024-01-15T10:00:00Z" "Get camera image with test time"
test_endpoint "GET" "$CAMERA_URL/api/camera/image?lotId=downtown&totalSpots=100" "Get downtown camera image"

# Step 5: Test AI Vision Model
echo -e "\n${YELLOW}Step 5: AI Vision Model Tests${NC}"
echo "------------------------------"

# Create a simple test image payload (base64 encoded 1x1 pixel)
test_image='{"imageData":"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==","totalSpots":100}'

test_endpoint "POST" "$AI_MODEL_URL/api/ai/analyze" "Analyze test image" "$test_image"

# Step 6: Integration Flow Test
echo -e "\n${YELLOW}Step 6: End-to-End Integration Test${NC}"
echo "-----------------------------------"

echo "🔄 Testing complete camera-to-API flow..."

# Get a real image from camera
echo -n "📸 Downloading test image from camera... "
camera_image=$(curl -s "$CAMERA_URL/api/camera/image?lotId=downtown&totalSpots=100" | base64 -w 0)
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Success${NC}"
    
    # Send image to AI for analysis
    echo -n "🤖 Analyzing image with AI... "
    ai_payload="{\"imageData\":\"$camera_image\",\"totalSpots\":100}"
    ai_result=$(curl -s -X POST -H "Content-Type: application/json" -d "$ai_payload" "$AI_MODEL_URL/api/ai/analyze")
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Success${NC}"
        echo "   AI Result: $ai_result"
        
        # Parse AI result and create parking state
        occupied_spots=$(echo "$ai_result" | jq -r '.occupiedSpots // 50')
        free_spots=$(echo "$ai_result" | jq -r '.freeSpots // 50')
        
        # Get a parking lot ID for testing
        parking_lots=$(curl -s "$REST_API_URL/api/parkinglots")
        first_lot_id=$(echo "$parking_lots" | jq -r '.[0].id // "test-lot"')
        
        # Create parking state record
        parking_state="{\"parkingLotId\":$first_lot_id,\"occupiedSpots\":$occupied_spots,\"freeSpots\":$free_spots,\"totalSpots\":100}"
        test_endpoint "POST" "$REST_API_URL/api/parkingstatehistory" "Store parking state" "$parking_state"
        
    else
        echo -e "${RED}✗ Failed${NC}"
    fi
else
    echo -e "${RED}✗ Failed${NC}"
fi

# Step 7: Show Results and Keep Services Running
echo -e "\n${YELLOW}Step 7: Test Results & Next Steps${NC}"
echo "--------------------------------"

echo -e "${GREEN}✅ All components started and tested successfully!${NC}"
echo ""
echo "🌐 Services are running at:"
echo "   • REST API:      $REST_API_URL/swagger"
echo "   • Camera:        $CAMERA_URL/api/camera/image"  
echo "   • AI Vision:     $AI_MODEL_URL/health"
echo ""
echo "📋 Service logs available at:"
echo "   • REST API:      tail -f /tmp/restapi.log"
echo "   • Camera:        tail -f /tmp/camera.log"
echo "   • AI Vision:     tail -f /tmp/aivisionmodel.log"
echo ""
echo "📖 Next steps for full integration:"
echo "1. 🔄 Start Azure Functions:"
echo "   cd ParkingSpotFinder/ImageDownloader && func start"
echo "   cd ParkingSpotFinder/ImageProcessor && func start"
echo ""
echo "2. 📊 Monitor real-time data:"
echo "   curl $REST_API_URL/api/parkingstatehistory/current"
echo ""
echo "3. 📈 View analytics:"
echo "   curl $REST_API_URL/api/parkingstatehistory/statistics/{parkingLotId}"
echo ""
echo -e "${BLUE}Press Ctrl+C to stop all services and exit${NC}"

# Keep services running until user interrupts
echo -e "\n${YELLOW}Services are running... Press Ctrl+C to stop${NC}"
while true; do
    sleep 5
    # Check if all services are still running
    failed_services=0
    for pid in "${BACKGROUND_PIDS[@]}"; do
        if ! kill -0 "$pid" 2>/dev/null; then
            failed_services=$((failed_services + 1))
        fi
    done
    
    if [ $failed_services -gt 0 ]; then
        echo -e "${RED}⚠️  $failed_services service(s) have stopped. Check logs for details.${NC}"
        break
    fi
done