---
name: dotnet-deployment
description: .NET deployment patterns including Docker containerization, multi-stage builds, dotnet publish configurations, health checks, and CI/CD pipeline integration.
---

# .NET Deployment Patterns

Comprehensive deployment strategies for .NET applications.

## When to Activate

- Containerizing .NET applications with Docker
- Publishing applications for production
- Setting up CI/CD pipelines
- Configuring health checks and readiness probes
- Implementing zero-downtime deployments
- Optimizing application startup and performance

## Docker Containerization

### Multi-Stage Dockerfile (Recommended)

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["Backend/Backend.csproj", "Backend/"]
RUN dotnet restore "Backend/Backend.csproj"

# Copy source code and build
COPY Backend/. Backend/
WORKDIR "/src/Backend"
RUN dotnet build "Backend.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "Backend.csproj" -c Release -o /app/publish \
    --no-restore \
    --runtime linux-x64 \
    --self-contained false \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create non-root user
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Copy published app
COPY --from=publish --chown=appuser /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Backend.dll"]
```

### Docker Compose for Development

```yaml
version: '3.8'

services:
  backend:
    build:
      context: .
      dockerfile: Backend/Dockerfile
    ports:
      - "5000:8080"
      - "5001:8081"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080;https://+:8081
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=myapp;Username=postgres;Password=postgres
      - Jwt__Secret=${JWT_SECRET}
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    networks:
      - app-network
    volumes:
      - ./logs:/app/logs

  postgres:
    image: postgres:15
    environment:
      - POSTGRES_DB=myapp
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - app-network

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 5
    networks:
      - app-network

networks:
  app-network:
    driver: bridge

volumes:
  postgres-data:
```

### .dockerignore

```
# Build artifacts
**/bin/
**/obj/
**/out/

# NuGet packages
**/packages/
*.nupkg

# IDE and editor files
**/.vs/
**/.vscode/
**/.idea/
*.user
*.suo

# Tests
**/TestResults/
**/*.Tests/

# OS files
**/.DS_Store
**/Thumbs.db

# Environment files
**/.env
**/.env.local

# Git
.git/
.gitignore
.gitattributes

# Documentation
*.md
docs/

# CI/CD
.github/
.gitlab-ci.yml
azure-pipelines.yml
```

## Publishing Configurations

### Framework-Dependent (Recommended for most scenarios)

```bash
# Smallest deployment size, requires .NET runtime on target
dotnet publish -c Release -o ./publish \
  --runtime linux-x64 \
  --no-self-contained

# Output: ~few MB + shared runtime
```

### Self-Contained (No .NET runtime required on target)

```bash
#dotnet runtime included, larger deployment size
dotnet publish -c Release -o ./publish \
  --runtime linux-x64 \
  --self-contained true

# Output: ~70-80 MB
```

### Single File (All in one executable)

```bash
# Everything bundled into a single executable
dotnet publish -c Release -o ./publish \
  --runtime linux-x64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true

# Output: Single ~80 MB file
```

### Trimmed (Smaller size, advanced)

```bash
# Remove unused code, aggressive optimization
dotnet publish -c Release -o ./publish \
  --runtime linux-x64 \
  --self-contained true \
  /p:PublishTrimmed=true \
  /p:TrimMode=link

# Output: ~40-50 MB
# ⚠️ Test thoroughly - can break reflection-based code
```

### ReadyToRun (Faster startup)

```bash
# AOT compilation for faster startup
dotnet publish -c Release -o ./publish \
  --runtime linux-x64 \
  /p:PublishReadyToRun=true

# Output: Larger size, faster startup
```

## Health Checks

### Basic Health Check

```csharp
// Program.cs
builder.Services.AddHealthChecks();

app.MapHealthChecks("/health");

// Returns: HTTP 200 "Healthy" or HTTP 503 "Unhealthy"
```

### Detailed Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgres",
        tags: new[] { "db", "sql" })
    .AddRedis(
        builder.Configuration["Redis:ConnectionString"]!,
        name: "redis",
        tags: new[] { "cache" })
    .AddUrlGroup(
        new Uri("https://api.example.com/health"),
        name: "external-api",
        tags: new[] { "external" });

// Detailed health endpoint with status
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.ToString()
            }),
            totalDuration = report.TotalDuration.ToString()
        });
        await context.Response.WriteAsync(result);
    }
});

// Separate liveness and readiness endpoints for Kubernetes
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false  // No checks, just returns 200 if app is running
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")  // Only run "ready" checks
});
```

### Custom Health Check

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;

    public DatabaseHealthCheck(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple query to check database connectivity
            await _context.Database.CanConnectAsync(cancellationToken);
            
            // Optional: Check if migrations are applied
            var pendingMigrations = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
            if (pendingMigrations.Any())
            {
                return HealthCheckResult.Degraded(
                    $"Database has {pendingMigrations.Count()} pending migrations");
            }

            return HealthCheckResult.Healthy("Database is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is not accessible", ex);
        }
    }
}

// Registration
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db", "ready" });
```

## Environment-Specific Configuration

```csharp
// appsettings.json (defaults)
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}

// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=myapp_dev;Username=dev;Password=dev"
  }
}

// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning"
    }
  }
  // ConnectionStrings set via environment variables
}

// Loading configuration
var builder = WebApplication.CreateBuilder(args);

// Adds appsettings.{Environment}.json
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.json",
    optional: true,
    reloadOnChange: true);

// Adds environment variables (highest priority)
builder.Configuration.AddEnvironmentVariables();

// Adds user secrets in Development
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}
```

## Kubernetes Deployment

### Deployment Manifest

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: backend-api
  labels:
    app: backend-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: backend-api
  template:
    metadata:
      labels:
        app: backend-api
    spec:
      containers:
      - name: backend-api
        image: myregistry.azurecr.io/backend-api:latest
        ports:
        - containerPort: 8080
          name: http
          protocol: TCP
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:8080"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: database-secret
              key: connection-string
        - name: Jwt__Secret
          valueFrom:
            secretKeyRef:
              name: jwt-secret
              key: secret
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 30
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 10
          timeoutSeconds: 3
          failureThreshold: 3
        startupProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 0
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 30
---
apiVersion: v1
kind: Service
metadata:
  name: backend-api
spec:
  selector:
    app: backend-api
  ports:
  - port: 80
    targetPort: 8080
    protocol: TCP
  type: ClusterIP
```

## GitHub Actions CI/CD

```yaml
name: .NET CI/CD

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

env:
  DOTNET_VERSION: '9.0.x'
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore Backend/Backend.csproj
    
    - name: Build
      run: dotnet build Backend/Backend.csproj --no-restore --configuration Release
    
    - name: Run tests
      run: dotnet test Backend.Tests/Backend.Tests.csproj --no-build --verbosity normal --configuration Release --collect:"XPlat Code Coverage"
    
    - name: Code Coverage Report
      uses: codecov/codecov-action@v3
      with:
        files: '**/coverage.cobertura.xml'
        fail_ci_if_error: true
    
    - name: Check code coverage threshold
      run: |
        dotnet test --no-build --configuration Release /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line
  
  docker-build-push:
    needs: build-and-test
    runs-on: ubuntu-latest
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    
    permissions:
      contents: read
      packages: write
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Log in to Container Registry
      uses: docker/login-action@v2
      with:
        registry: ${{ env.REGISTRY }}
        username: ${{ github.actor }}
        password: ${{ secrets.GITHUB_TOKEN }}
    
    - name: Extract metadata
      id: meta
      uses: docker/metadata-action@v4
      with:
        images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
        tags: |
          type=ref,event=branch
          type=semver,pattern={{version}}
          type=sha,prefix={{branch}}-
    
    - name: Build and push Docker image
      uses: docker/build-push-action@v4
      with:
        context: .
        file: Backend/Dockerfile
        push: true
        tags: ${{ steps.meta.outputs.tags }}
        labels: ${{ steps.meta.outputs.labels }}
        cache-from: type=gha
        cache-to: type=gha,mode=max
  
  deploy-to-kubernetes:
    needs: docker-build-push
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Set up kubectl
      uses: azure/setup-kubectl@v3
    
    - name: Configure kubectl
      run: |
        echo "${{ secrets.KUBE_CONFIG }}" | base64 -d > kubeconfig
        export KUBECONFIG=kubeconfig
    
    - name: Deploy to Kubernetes
      run: |
        kubectl set image deployment/backend-api \
          backend-api=${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }} \
          --record
        
        kubectl rollout status deployment/backend-api
```

## Performance Optimization

### Startup Performance

```csharp
// Program.cs optimizations

// 1. Use source generators for JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// 2. Disable developer exception page in production
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
else
{
    app.UseDeveloperExceptionPage();
}

// 3. Enable response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

app.UseResponseCompression();

// 4. Configure Kestrel for production
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
    serverOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
    serverOptions.Limits.MaxConcurrentConnections = 100;
});
```

### Database Connection Pooling

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            npgsqlOptions.CommandTimeout(30);
            npgsqlOptions.MinBatchSize(1);
            npgsqlOptions.MaxBatchSize(100);
        });
    
    // Connection pooling is enabled by default
    // Configure via connection string: "Pooling=true;MinPoolSize=0;MaxPoolSize=100"
});
```

## Deployment Checklist

- [ ] Docker multi-stage build configured
- [ ] .dockerignore file excludes unnecessary files
- [ ] Non-root user created in Dockerfile
- [ ] Health checks implemented (/health/live, /health/ready)
- [ ] Environment-specific configuration separated
- [ ] Secrets managed via environment variables (never in code)
- [ ] Logging configured appropriately per environment
- [ ] Response compression enabled
- [ ] Security headers configured
- [ ] HTTPS enforced in production
- [ ] Database connection pooling configured
- [ ] Resources limits set in Kubernetes
- [ ] CI/CD pipeline includes tests and coverage checks
- [ ] Rollback strategy defined

## Related Resources

- See [skills/docker-patterns](../docker-patterns/SKILL.md) for Docker best practices
- See [skills/deployment-patterns](../deployment-patterns/SKILL.md) for general deployment strategies
- See [rules/csharp/patterns.md](../../rules/csharp/patterns.md) for configuration patterns
