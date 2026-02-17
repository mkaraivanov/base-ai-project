#!/bin/bash

echo "=== Testing Timezone Fix ==="
echo ""
echo "This test verifies that DateTime values from the backend are properly serialized with UTC timezone."
echo ""

# Kill any existing backend process
echo "Stopping any existing backend processes..."
lsof -ti:5076 | xargs kill -9 2>/dev/null || true
sleep 2

# Start backend
echo "Starting backend..."
cd Backend
dotnet run --no-build > /tmp/backend.log 2>&1 &
BACKEND_PID=$!
cd ..

# Wait for backend to start
echo "Waiting for backend to  start..."
sleep 5

# Register a test user
echo ""
echo "1. Registering test user..."
REGISTER_RESPONSE=$(curl -s -X POST http://localhost:5076/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test-timezone@example.com",
    "password": "Test123!",
    "confirmPassword": "Test123!",
    "firstName": "Test",
    "lastName": "User",
    "phoneNumber": "1234567890"
  }')

echo "Register response:"
echo "$REGISTER_RESPONSE" | python3 -c "import sys, json; data=json.load(sys.stdin); print(json.dumps(data, indent=2))"

TOKEN=$(echo "$REGISTER_RESPONSE" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data.get('data', {}).get('token', ''))" 2>/dev/null)

if [ -z "$TOKEN" ]; then
  echo "ERROR: Failed to get auth token"
  kill $BACKEND_PID 2>/dev/null
  exit 1
fi

# Get showtimes to find a valid showtime ID
echo ""
echo "2. Getting showtimes..."
SHOWTIMES=$(curl -s http://localhost:5076/api/showtimes \
  -H "Authorization: Bearer $TOKEN")

SHOWTIME_ID=$(echo "$SHOWTIMES" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data.get('data', [{}])[0].get('id', ''))" 2>/dev/null)

if [ -z "$SHOWTIME_ID" ]; then
  echo "ERROR: No showtimes available"
  kill $BACKEND_PID 2>/dev/null
  exit 1
fi

# Create a reservation
echo ""
echo "3. Creating reservation..."
RESERVE_RESPONSE=$(curl -s -X POST http://localhost:5076/api/bookings/reserve \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"showtimeId\": \"$SHOWTIME_ID\",
    \"seatNumbers\": [\"A1\", \"A2\"]
  }")

echo "Reservation response:"
echo "$RESERVE_RESPONSE" | python3 -c "import sys, json; data=json.load(sys.stdin); print(json.dumps(data, indent=2))"

# Extract and check ExpiresAt field
EXPIRES_AT=$(echo "$RESERVE_RESPONSE" | python3 -c "import sys, json; data=json.load(sys.stdin); print(data.get('data', {}).get('expiresAt', ''))" 2>/dev/null)

echo ""
echo "=== Verification ==="
echo "ExpiresAt value: $EXPIRES_AT"

# Check if it ends with 'Z' (UTC indicator)
if [[ "$EXPIRES_AT" == *Z ]]; then
  echo "✅ SUCCESS: ExpiresAt is properly formatted with UTC timezone (ends with 'Z')"
  RESULT=0
else
  echo "❌ FAIL: ExpiresAt does not have UTC timezone indicator"
  RESULT=1
fi

# Cleanup
echo ""
echo "Cleaning up..."
kill $BACKEND_PID 2>/dev/null
sleep 1

exit $RESULT
