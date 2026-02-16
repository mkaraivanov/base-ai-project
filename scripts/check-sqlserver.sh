#!/bin/bash

# Check SQL Server container status
if docker ps | grep cinema-sqlserver > /dev/null; then
    echo "✅ SQL Server container is running"
    docker ps --filter name=cinema-sqlserver --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
else
    echo "❌ SQL Server container is not running"
    echo "Run: ./scripts/start-sqlserver.sh"
    exit 1
fi
