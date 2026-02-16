#!/bin/bash

# Start SQL Server in Docker for local development
docker run -e "ACCEPT_EULA=Y" \
  -e "SA_PASSWORD=YourStrong@Passw0rd123" \
  -p 1433:1433 \
  --name cinema-sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest

echo "✅ SQL Server container started"
echo "Server: localhost,1433"
echo "User: sa"
echo "Password: YourStrong@Passw0rd123"
