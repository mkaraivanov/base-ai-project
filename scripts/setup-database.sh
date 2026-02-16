#!/bin/bash

set -e

echo "🚀 Setting up database..."

# Check if SQL Server is running
if ! docker ps | grep cinema-sqlserver > /dev/null; then
    echo "❌ SQL Server container is not running"
    echo "Starting SQL Server container..."
    ./scripts/start-sqlserver.sh
    echo "⏳ Waiting for SQL Server to be ready (15 seconds)..."
    sleep 15
fi

# Apply migrations
echo "📦 Applying database migrations..."
cd Backend
dotnet ef database update --project ../Backend/Infrastructure --startup-project .

echo "✅ Database setup complete!"
echo ""
echo "Connection Details:"
echo "  Server: localhost,1433"
echo "  Database: CinemaBookingDb_Dev (Development)"
echo "  User: sa"
echo "  Password: YourStrong@Passw0rd123"
