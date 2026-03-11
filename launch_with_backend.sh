#!/bin/bash

# Holiday Scheduler launcher with backend support
# Usage: ./launch_with_backend.sh [backend_url]
# Example: ./launch_with_backend.sh https://my-backend.railway.app

BACKEND_URL="${1:-}"

if [ -z "$BACKEND_URL" ]; then
    echo "Usage: $0 <backend_url>"
    echo "Example: $0 https://holiday-scheduler-backend-prod.up.railway.app"
    echo ""
    echo "Or set HOLIDAY_SUPPORT_ENDPOINT environment variable:"
    echo "  export HOLIDAY_SUPPORT_ENDPOINT=https://your-backend.com"
    echo "  /Applications/Holiday\ Scheduler\ Demonstrator.app/Contents/MacOS/Holiday\ Scheduler\ Demonstrator"
    exit 1
fi

# Validate URL format
if [[ ! "$BACKEND_URL" =~ ^https?:// ]]; then
    echo "Error: Backend URL must start with http:// or https://"
    exit 1
fi

# Test backend connectivity
echo "Testing backend connectivity at $BACKEND_URL..."
if curl -s -f "$BACKEND_URL/health" > /dev/null 2>&1; then
    echo "✓ Backend is online"
else
    echo "✗ Warning: Backend not reachable. App will still work, but crashes won't be sent."
    read -p "Continue anyway? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

echo "Launching Holiday Scheduler with backend: $BACKEND_URL"
export HOLIDAY_SUPPORT_ENDPOINT="$BACKEND_URL"
'/Applications/Holiday Scheduler Demonstrator.app/Contents/MacOS/Holiday Scheduler Demonstrator'
