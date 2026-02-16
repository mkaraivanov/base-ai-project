# macOS Setup Guide

## Problem

SQL Server LocalDB is Windows-only and won't work on macOS. You'll see this error:

```
LocalDB is not supported on this platform.
```

## Solution

Use Docker to run SQL Server on macOS.

---

## Prerequisites

### 1. Install Docker

Choose one option:

**Option A: Docker Desktop (Recommended for beginners)**
```bash
brew install --cask docker
```
Then open Docker Desktop from Applications.

**Option B: Colima (Lightweight alternative)**
```bash
brew install docker colima docker-compose
colima start
```

Verify Docker is working:
```bash
docker --version
docker ps
```

---

## Quick Start

### 1. Start SQL Server

```bash
./scripts/start-sqlserver.sh
```

This will:
- Start SQL Server 2022 container
- Expose port 1433
- Use password: `YourStrong@Passw0rd123`

### 2. Setup Database

Wait 15 seconds for SQL Server to initialize, then:

```bash
./scripts/setup-database.sh
```

This will:
- Apply EF Core migrations
- Create the database
- Setup tables

### 3. Run the API

```bash
cd Backend
dotnet run
```

The API will be available at: `https://localhost:5000`

---

## Helper Scripts

All scripts are in the `scripts/` directory:

| Script | Purpose |
|--------|---------|
| `start-sqlserver.sh` | Start SQL Server Docker container |
| `stop-sqlserver.sh` | Stop and remove SQL Server container |
| `check-sqlserver.sh` | Check if SQL Server is running |
| `setup-database.sh` | Apply migrations and setup database |

---

## Connection Details

**Development:**
- Server: `localhost,1433`
- Database: `CinemaBookingDb_Dev`
- User: `sa`
- Password: `YourStrong@Passw0rd123`

**Connection String:**
```
Server=localhost,1433;Database=CinemaBookingDb_Dev;User Id=sa;Password=YourStrong@Passw0rd123;TrustServerCertificate=true;Encrypt=false
```

---

## Troubleshooting

### Docker not found

```bash
# Install Docker
brew install --cask docker

# Or use Colima
brew install docker colima
colima start
```

### Port 1433 already in use

```bash
# Check what's using the port
lsof -i :1433

# Stop existing SQL Server container
docker stop cinema-sqlserver
docker rm cinema-sqlserver
```

### Database connection fails

```bash
# Check SQL Server is running
./scripts/check-sqlserver.sh

# Check Docker logs
docker logs cinema-sqlserver

# Restart container
./scripts/stop-sqlserver.sh
./scripts/start-sqlserver.sh

# Wait 15 seconds then try again
sleep 15
./scripts/setup-database.sh
```

### Migrations fail

```bash
# Remove existing database and recreate
cd Backend
dotnet ef database drop --force --project ../Backend/Infrastructure --startup-project .
dotnet ef database update --project ../Backend/Infrastructure --startup-project .
```

---

## Production Setup

**⚠️ IMPORTANT:** The default password is for **development only**.

For production:

1. **Use environment variables:**
   ```bash
   export DB_PASSWORD="your-strong-password"
   ```

2. **Update connection string:**
   ```json
   "DefaultConnection": "Server=your-server;Database=CinemaBookingDb;User Id=sa;Password=${DB_PASSWORD};TrustServerCertificate=true"
   ```

3. **Use Azure SQL Database or AWS RDS** instead of Docker containers

4. **Enable SSL/TLS encryption** in production

---

## Next Steps

Once SQL Server is running:

1. ✅ Apply migrations: `./scripts/setup-database.sh`
2. ✅ Run the API: `cd Backend && dotnet run`
3. ✅ Test endpoints:
   - Register: `POST /api/auth/register`
   - Login: `POST /api/auth/login`
4. ✅ Run tests: `dotnet test`

See [Phase 1 Documentation](phases/phase-1-foundation.md) for full implementation details.
