# EziOrm

EziOrm is a lightweight, custom Object-Relational Mapping (ORM) framework for .NET applications. It provides an easy-to-use interface for database operations while offering flexibility and control over your data access layer.

## Features

### 1. DbContext

The `DbContext` class is the central point of database operations in EziOrm.

#### Usage:

```csharp
public class MyDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }

    public MyDbContext(string connectionString) : base(connectionString) { }
}

// Using the context
using (var context = new MyDbContext("YourConnectionString"))
{
    var users = await context.Users.Where(u => u.Age > 18).ToListAsync();
}
```
### 2. Entity Mapping
EziOrm uses attributes for flexible entity-to-table mapping.
Usage:
```csharp
[Table("Users")]
public class User
{
    [PrimaryKey(Identity = true)]
    public int Id { get; set; }

    [Column("UserName")]
    public string Name { get; set; }

    [Ignore]
    public int TempProperty { get; set; }
}
```


### 3. LINQ-like Query Syntax
EziOrm supports a LINQ-like syntax for querying entities.
Usage:
```csharp
var adultUsers = await context.Users
    .Where(u => u.Age > 18)
    .OrderBy(u => u.Name)
    .Take(10)
    .ToListAsync();
```

### 4. Change Tracking
EziOrm includes a change tracking system to manage entity states.
Usage:
```csharp
var user = new User { Name = "John Doe" };
context.Users.Add(user);
await context.SaveChangesAsync();
```
### 5. Relationship Management
EziOrm supports configuring and managing entity relationships.
Usage:
```csharp
protected override void ConfigureRelationships()
{
    OneToMany<User, Order>(u => u.Orders, o => o.User);
    ManyToMany<User, Role>(u => u.Roles, r => r.Users, "UserRoles");
}
```


### Getting Started

Install EziOrm via NuGet (package not yet available)
Create your DbContext class
Define your entity classes
Use EziOrm in your application

## Known Issues and Limitations

Error Handling: Limited error handling throughout the code. More robust error handling and logging should be implemented.
Connection Management: The ConnectionManager class doesn't implement connection pooling, which could lead to performance issues with many concurrent requests.
SQL Injection Vulnerability: The QueryBuilder class generates SQL strings directly from user input, which could be vulnerable to SQL injection attacks. Parameterized queries should be implemented.
Limited Support for Complex Queries: The current implementation doesn't support joins or subqueries, which limits its functionality for complex data retrieval scenarios.
Lack of Caching Mechanism: There's no built-in caching system, which could impact performance for frequently accessed data.
Limited Database Support: The ORM seems to be designed primarily for SQL Server. Support for other databases should be considered.
# Features Will Impl
 - Caching
 - Lazy Loading & Eager Loading
 - Tracking and No-Tracking
 - Advanced Query Building
 - Logging and Diagnostics
 - Transactions
# Suggested Improvements

 Implement comprehensive error handling and logging throughout the framework.
Introduce connection pooling in the ConnectionManager class to improve performance.
Refactor the QueryBuilder to use parameterized queries to prevent SQL injection.
Extend query capabilities to support joins and subqueries.
Implement a caching mechanism for frequently accessed data.
Add support for multiple database systems.
Implement unit tests to ensure reliability and ease future development.
Consider adding support for migrations to manage database schema changes.
