---
name: efcore-patterns
description: Entity Framework Core patterns for database operations, migrations, query optimization, DbContext configuration, and data access best practices.
---

# Entity Framework Core Patterns

Comprehensive patterns for working with Entity Framework Core in .NET applications.

## When to Activate

- Configuring DbContext and entity mappings
- Creating or reviewing database migrations
- Optimizing queries and preventing N+1 problems
- Implementing repository pattern with EF Core
- Handling concurrency and transactions
- Working with relationships and navigation properties
- Implementing soft deletes with global query filters

## DbContext Configuration

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filter for soft delete
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        modelBuilder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Enable sensitive data logging in development only
        #if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
        #endif
    }

    // Override SaveChangesAsync for audit fields
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }

            entity.UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

// Registration in Program.cs
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
        });
    
    // Only in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});
```

## Entity Configuration with Fluent API

```csharp
public class User : BaseEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    
    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public UserProfile? Profile { get; set; }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.HasIndex(u => u.Email)
            .IsUnique();
        
        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);
        
        // Relationships
        builder.HasMany(u => u.Orders)
            .WithOne(o => o.User)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Row versioning for concurrency
        builder.Property(u => u.RowVersion)
            .IsRowVersion();
    }
}

// Alternative: Data Annotations (less preferred)
public class UserWithAnnotations
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
```

## Query Optimization Patterns

### AsNoTracking for Read-Only Queries

```csharp
// ✅ GOOD: Use AsNoTracking for read-only queries
public async Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken)
{
    return await _context.Users
        .AsNoTracking()  // Faster, no change tracking overhead
        .Where(u => u.IsActive)
        .ToListAsync(cancellationToken);
}

// ❌ BAD: Tracking entities unnecessarily
public async Task<IEnumerable<User>> GetActiveUsers()
{
    return await _context.Users  // Tracked by default, slower
        .Where(u => u.IsActive)
        .ToListAsync();
}
```

### Prevent N+1 Queries with Include

```csharp
// ❌ BAD: N+1 query problem
public async Task<List<User>> GetUsersWithOrders()
{
    var users = await _context.Users.ToListAsync();
    
    foreach (var user in users)  // Each iteration makes a new query!
    {
        var orders = await _context.Orders
            .Where(o => o.UserId == user.Id)
            .ToListAsync();
    }
    
    return users;
}

// ✅ GOOD: Use Include to load related data in one query
public async Task<List<User>> GetUsersWithOrders(CancellationToken cancellationToken)
{
    return await _context.Users
        .Include(u => u.Orders)  // Loads orders in same query
        .AsNoTracking()
        .ToListAsync(cancellationToken);
}

// ✅ BETTER: Use AsSplitQuery for large collections
public async Task<List<User>> GetUsersWithOrdersAndProducts(CancellationToken cancellationToken)
{
    return await _context.Users
        .Include(u => u.Orders)
            .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
        .AsSplitQuery()  // Prevents cartesian explosion
        .AsNoTracking()
        .ToListAsync(cancellationToken);
}
```

### Projection with Select

```csharp
// ❌ BAD: Loading entire entities
public async Task<List<UserListDto>> GetUserList()
{
    var users = await _context.Users.ToListAsync();
    return users.Select(u => new UserListDto
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email
    }).ToList();
}

// ✅ GOOD: Project directly in database query
public async Task<List<UserListDto>> GetUserList(CancellationToken cancellationToken)
{
    return await _context.Users
        .Select(u => new UserListDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email
        })
        .ToListAsync(cancellationToken);
}
```

### Compiled Queries for Performance

```csharp
private static readonly Func<AppDbContext, Guid, CancellationToken, Task<User?>> _getUserById =
    EF.CompileAsyncQuery((AppDbContext context, Guid id, CancellationToken ct) =>
        context.Users
            .AsNoTracking()
            .FirstOrDefault(u => u.Id == id));

public async Task<User?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken)
{
    return await _getUserById(_context, id, cancellationToken);
}
```

## Migration Best Practices

### Safe Column Addition

```csharp
// ✅ GOOD: Adding nullable column (safe, no data loss)
migrationBuilder.AddColumn<string>(
    name: "PhoneNumber",
    table: "Users",
    type: "varchar(20)",
    nullable: true);

// ❌ RISKY: Adding required column without default
migrationBuilder.AddColumn<string>(
    name: "PhoneNumber",
    table: "Users",
    type: "varchar(20)",
    nullable: false);  // Fails if table has data!

// ✅ BETTER: Adding required column with default value
migrationBuilder.AddColumn<string>(
    name: "PhoneNumber",
    table: "Users",
    type: "varchar(20)",
    nullable: false,
    defaultValue: "000-000-0000");
```

### Safe Column Removal (Multi-Step Migration)

```csharp
// Step 1: Make column nullable (deploy to production)
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AlterColumn<string>(
        name: "OldColumn",
        table: "Users",
        nullable: true);  // Was false before
}

// Step 2: Remove column from code, stop using it (deploy application)
// ... wait for all instances to update ...

// Step 3: Drop column (new migration)
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(
        name: "OldColumn",
        table: "Users");
}
```

### Data Migration

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Add new column
    migrationBuilder.AddColumn<string>(
        name: "FullName",
        table: "Users",
        nullable: true);

    // Migrate data
    migrationBuilder.Sql(@"
        UPDATE Users 
        SET FullName = CONCAT(FirstName, ' ', LastName)
        WHERE FullName IS NULL
    ");

    // Make column required
    migrationBuilder.AlterColumn<string>(
        name: "FullName",
        table: "Users",
        nullable: false);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(name: "FullName", table: "Users");
}
```

### Generate Migration Script for Review

```bash
# Generate SQL script for migration
dotnet ef migrations script --idempotent --output migration.sql

# Review SQL before applying
cat migration.sql

# Apply to production database
dotnet ef database update --connection "ProductionConnectionString"
```

## Global Query Filters (Soft Delete)

```csharp
public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

// Configure in DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Apply to all entities inheriting from BaseEntity
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
        {
            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(GetSoftDeleteFilter(entityType.ClrType));
        }
    }
}

private static LambdaExpression GetSoftDeleteFilter(Type entityType)
{
    var parameter = Expression.Parameter(entityType, "e");
    var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
    var condition = Expression.Equal(property, Expression.Constant(false));
    return Expression.Lambda(condition, parameter);
}

// Soft delete implementation
public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken)
{
    var user = await _context.Users.FindAsync([id], cancellationToken);
    if (user is not null)
    {
        user.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

// Query deleted records
public async Task<List<User>> GetDeletedUsersAsync(CancellationToken cancellationToken)
{
    return await _context.Users
        .IgnoreQueryFilters()  // Override global filter
        .Where(u => u.IsDeleted)
        .ToListAsync(cancellationToken);
}
```

## Concurrency Control

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    [Timestamp]
    public byte[]? RowVersion { get; set; }  // Optimistic concurrency token
}

// Handle concurrency conflicts
public async Task<Result> UpdateUserAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken)
{
    try
    {
        var user = await _context.Users.FindAsync([id], cancellationToken);
        if (user is null)
        {
            return Result.Failure("User not found");
        }

        user.Name = dto.Name;
        user.Email = dto.Email;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
    catch (DbUpdateConcurrencyException)
    {
        return Result.Failure("The record was modified by another user. Please refresh and try again.");
    }
}
```

## Value Objects and Owned Entities

```csharp
public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Address Address { get; set; } = null!;
}

// Configuration
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.OwnsOne(u => u.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("AddressStreet");
            address.Property(a => a.City).HasColumnName("AddressCity");
            address.Property(a => a.State).HasColumnName("AddressState");
            address.Property(a => a.ZipCode).HasColumnName("AddressZipCode");
        });
    }
}
```

## Transactions

```csharp
public async Task<Result> TransferFundsAsync(
    Guid fromAccountId, 
    Guid toAccountId, 
    decimal amount, 
    CancellationToken cancellationToken)
{
    using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    
    try
    {
        var fromAccount = await _context.Accounts.FindAsync([fromAccountId], cancellationToken);
        var toAccount = await _context.Accounts.FindAsync([toAccountId], cancellationToken);

        if (fromAccount is null || toAccount is null)
        {
            return Result.Failure("Account not found");
        }

        if (fromAccount.Balance < amount)
        {
            return Result.Failure("Insufficient funds");
        }

        fromAccount.Balance -= amount;
        toAccount.Balance += amount;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(cancellationToken);
        _logger.LogError(ex, "Error transferring funds");
        return Result.Failure("Transfer failed");
    }
}
```

## Checklist

- [ ] DbContext registered with appropriate lifetime (Scoped)
- [ ] Entity configurations use Fluent API in separate classes
- [ ] Indexes created on foreign keys and frequently queried columns
- [ ] AsNoTracking used for read-only queries
- [ ] Include/ThenInclude used to prevent N+1 queries
- [ ] Projections used when not all entity data needed
- [ ] Global query filters configured for soft delete
- [ ] Migrations reviewed for data safety
- [ ] Row versioning configured for concurrency control
- [ ] Transactions used for multi-step operations
- [ ] All queries have CancellationToken parameter

## Related Resources

- See [rules/csharp/patterns.md](../../rules/csharp/patterns.md) for repository pattern details
- See [skills/dotnet-patterns](../dotnet-patterns/SKILL.md) for service layer integration
- See [skills/database-migrations](../database-migrations/SKILL.md) for migration strategies
- Use [agents/efcore-reviewer.md](../../agents/efcore-reviewer.md) for EF Core code review
