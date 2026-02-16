#!/bin/bash

# Stop and remove SQL Server container
docker stop cinema-sqlserver 2>/dev/null
docker rm cinema-sqlserver 2>/dev/null

echo "✅ SQL Server container stopped and removed"
