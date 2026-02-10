#!/bin/bash

# API endpoint configuration
API_BASE_URL="http://localhost:5000"
API_BASE_URL_HTTPS="https://localhost:5001"

# User credentials (modify these for your test user)
EMAIL="test@example.com"
PASSWORD="TestPassword123!"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}WatchDog Alarm API Test Script${NC}"
echo "=================================="
echo "Base URL: $API_BASE_URL"
echo "Email: $EMAIL"
echo ""

# Function to check if API is running
check_api() {
    echo -e "${YELLOW}Checking if API is running...${NC}"
    
    # Try HTTP first
    if curl -s -f "$API_BASE_URL/api/auth/me" > /dev/null 2>&1; then
        BASE_URL="$API_BASE_URL"
        echo -e "${GREEN}✓ API is running on HTTP${NC}"
        return 0
    fi
    
    # Try HTTPS
    if curl -s -k -f "$API_BASE_URL_HTTPS/api/auth/me" > /dev/null 2>&1; then
        BASE_URL="$API_BASE_URL_HTTPS"
        echo -e "${GREEN}✓ API is running on HTTPS${NC}"
        return 0
    fi
    
    echo -e "${RED}✗ API is not running or not accessible${NC}"
    echo "Please start the API first with: dotnet run --project src/WdAlarm.Api"
    exit 1
}

# Function to login and get JWT token
login() {
    echo -e "${YELLOW}Attempting to login...${NC}"
    
    LOGIN_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X POST \
        "$BASE_URL/api/auth/login" \
        -H "Content-Type: application/json" \
        -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}")
    
    HTTP_CODE=$(echo "$LOGIN_RESPONSE" | tail -n1)
    RESPONSE_BODY=$(echo "$LOGIN_RESPONSE" | head -n -1)
    
    if [ "$HTTP_CODE" = "200" ]; then
        ACCESS_TOKEN=$(echo "$RESPONSE_BODY" | jq -r '.accessToken')
        EXPIRES_IN=$(echo "$RESPONSE_BODY" | jq -r '.expiresIn')
        USER_ID=$(echo "$RESPONSE_BODY" | jq -r '.userId')
        
        echo -e "${GREEN}✓ Login successful${NC}"
        echo "User ID: $USER_ID"
        echo "Token expires in: $EXPIRES_IN seconds"
        echo ""
        
        # Export token for use in subsequent commands
        export JWT_TOKEN="$ACCESS_TOKEN"
        return 0
    else
        echo -e "${RED}✗ Login failed (HTTP $HTTP_CODE)${NC}"
        echo "Response: $RESPONSE_BODY"
        echo ""
        echo "Possible solutions:"
        echo "1. Check if the user exists: curl -X POST \"$BASE_URL/api/auth/register\" -H \"Content-Type: application/json\" -d '{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\",\"firstName\":\"Test\",\"lastName\":\"User\"}'"
        echo "2. Verify the credentials are correct"
        exit 1
    fi
}

# Function to get user alarms
get_alarms() {
    echo -e "${YELLOW}Fetching user alarms...${NC}"
    
    ALARMS_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X GET \
        "$BASE_URL/api/alarms" \
        -H "Authorization: Bearer $JWT_TOKEN" \
        -H "Content-Type: application/json")
    
    HTTP_CODE=$(echo "$ALARMS_RESPONSE" | tail -n1)
    RESPONSE_BODY=$(echo "$ALARMS_RESPONSE" | head -n -1)
    
    if [ "$HTTP_CODE" = "200" ]; then
        ALARM_COUNT=$(echo "$RESPONSE_BODY" | jq '. | length')
        echo -e "${GREEN}✓ Found $ALARM_COUNT alarm(s)${NC}"
        echo ""
        echo "Alarm Details:"
        echo "=============="
        
        if [ "$ALARM_COUNT" -gt 0 ]; then
            echo "$RESPONSE_BODY" | jq -r '.[] | "ID: \(.id), Name: \(.name), Status: \(.isActive), Next Alarm: \(.nextAlarmPoint // "N/A")"'
        else
            echo "No alarms found for this user."
        fi
    else
        echo -e "${RED}✗ Failed to fetch alarms (HTTP $HTTP_CODE)${NC}"
        echo "Response: $RESPONSE_BODY"
    fi
}

# Function to get current user info
get_user_info() {
    echo -e "${YELLOW}Fetching current user info...${NC}"
    
    USER_RESPONSE=$(curl -s -w "\n%{http_code}" \
        -X GET \
        "$BASE_URL/api/auth/me" \
        -H "Authorization: Bearer $JWT_TOKEN" \
        -H "Content-Type: application/json")
    
    HTTP_CODE=$(echo "$USER_RESPONSE" | tail -n1)
    RESPONSE_BODY=$(echo "$USER_RESPONSE" | head -n -1)
    
    if [ "$HTTP_CODE" = "200" ]; then
        EMAIL=$(echo "$RESPONSE_BODY" | jq -r '.email')
        USER_ID=$(echo "$RESPONSE_BODY" | jq -r '.userId')
        FIRST_NAME=$(echo "$RESPONSE_BODY" | jq -r '.firstName')
        LAST_NAME=$(echo "$RESPONSE_BODY" | jq -r '.lastName')
        
        echo -e "${GREEN}✓ User info retrieved${NC}"
        echo "User ID: $USER_ID"
        echo "Email: $EMAIL"
        echo "Name: $FIRST_NAME $LAST_NAME"
    else
        echo -e "${RED}✗ Failed to fetch user info (HTTP $HTTP_CODE)${NC}"
        echo "Response: $RESPONSE_BODY"
    fi
}

# Main execution
echo "Starting API test..."
echo ""

# Check dependencies
if ! command -v jq &> /dev/null; then
    echo -e "${RED}Error: jq is required but not installed${NC}"
    echo "Please install jq: sudo apt-get install jq (Ubuntu/Debian) or brew install jq (macOS)"
    exit 1
fi

# Check if API is running
check_api

# Login to get JWT token
login

# Get user info
get_user_info
echo ""

# Get user alarms
get_alarms
echo ""

echo -e "${GREEN}✓ Script completed successfully!${NC}"
echo ""
echo "Your JWT token is stored in the JWT_TOKEN environment variable."
echo "You can use it for manual API testing:"
echo "curl -H \"Authorization: Bearer \$JWT_TOKEN\" \"$BASE_URL/api/alarms\""