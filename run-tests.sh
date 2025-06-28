#!/bin/bash

# ParkingSpotFinder Test Runner with Dependency Checks
# This script verifies prerequisites and runs the full integration test

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 ParkingSpotFinder Test Runner${NC}"
echo "================================="

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to check version
check_version() {
    local tool=$1
    local version_cmd=$2
    local expected=$3
    
    echo -n "  Checking $tool version... "
    if command_exists "$tool"; then
        local version=$($version_cmd 2>&1 | head -n1)
        echo -e "${GREEN}✓ Found${NC} ($version)"
        return 0
    else
        echo -e "${RED}✗ Not found${NC}"
        return 1
    fi
}

# Step 1: Check prerequisites
echo -e "\n${YELLOW}Step 1: Checking Prerequisites${NC}"
echo "------------------------------"

missing_deps=false

# Check .NET
if ! check_version "dotnet" "dotnet --version" "9.0"; then
    echo -e "${RED}  Please install .NET 9.0 SDK: https://dotnet.microsoft.com/download${NC}"
    missing_deps=true
fi

# Check curl
if ! check_version "curl" "curl --version" ""; then
    echo -e "${RED}  Please install curl${NC}"
    missing_deps=true
fi

# Check jq
if ! check_version "jq" "jq --version" ""; then
    echo -e "${RED}  Please install jq: https://jqlang.github.io/jq/download/${NC}"
    missing_deps=true
fi

# Check base64
if ! check_version "base64" "base64 --version" ""; then
    echo -e "${RED}  base64 utility not found${NC}"
    missing_deps=true
fi

if [ "$missing_deps" = true ]; then
    echo -e "\n${RED}❌ Missing dependencies. Please install the required tools and try again.${NC}"
    exit 1
fi

echo -e "\n${GREEN}✅ All prerequisites satisfied!${NC}"

# Step 2: Check project structure
echo -e "\n${YELLOW}Step 2: Verifying Project Structure${NC}"
echo "-----------------------------------"

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PARKING_ROOT="$PROJECT_ROOT/ParkingSpotFinder"

required_dirs=(
    "$PARKING_ROOT/RestApi"
    "$PARKING_ROOT/Camera"
    "$PARKING_ROOT/AiVisionModel"
    "$PARKING_ROOT/Database"
)

for dir in "${required_dirs[@]}"; do
    if [ -d "$dir" ]; then
        echo -e "  ${GREEN}✓${NC} $dir"
    else
        echo -e "  ${RED}✗${NC} $dir (missing)"
        missing_deps=true
    fi
done

if [ "$missing_deps" = true ]; then
    echo -e "\n${RED}❌ Project structure incomplete. Please ensure all components are present.${NC}"
    exit 1
fi

# Check for test data
if [ -f "$PROJECT_ROOT/test-data.json" ]; then
    echo -e "  ${GREEN}✓${NC} test-data.json"
else
    echo -e "  ${RED}✗${NC} test-data.json (missing)"
    missing_deps=true
fi

if [ "$missing_deps" = true ]; then
    echo -e "\n${RED}❌ Missing test data file. Please ensure test-data.json exists.${NC}"
    exit 1
fi

echo -e "\n${GREEN}✅ Project structure verified!${NC}"

# Step 3: Build all projects
echo -e "\n${YELLOW}Step 3: Building Projects${NC}"
echo "-------------------------"

for dir in "${required_dirs[@]}"; do
    project_name=$(basename "$dir")
    echo -n "  Building $project_name... "
    
    if (cd "$dir" && dotnet build --verbosity quiet > /dev/null 2>&1); then
        echo -e "${GREEN}✓ Success${NC}"
    else
        echo -e "${RED}✗ Failed${NC}"
        echo -e "${RED}    Build failed for $project_name. Run 'dotnet build' in $dir for details.${NC}"
        exit 1
    fi
done

echo -e "\n${GREEN}✅ All projects built successfully!${NC}"

# Step 4: Run the integration test
echo -e "\n${YELLOW}Step 4: Running Integration Test${NC}"
echo "--------------------------------"

if [ -x "$PROJECT_ROOT/test-system.sh" ]; then
    echo "Starting integration test script..."
    exec "$PROJECT_ROOT/test-system.sh"
else
    echo -e "${RED}❌ test-system.sh not found or not executable${NC}"
    echo "Make sure to run: chmod +x test-system.sh"
    exit 1
fi