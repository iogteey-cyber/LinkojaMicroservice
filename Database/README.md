# Database Scripts

This directory contains SQL scripts for setting up the Linkoja Microservice database.

## Files

### `CreateTables.sql`
Complete database schema creation script for PostgreSQL. This script:
- Uses `CREATE TABLE IF NOT EXISTS` for idempotent execution
- Creates all 11 tables with proper relationships
- Adds indexes for performance optimization
- Includes foreign key constraints
- Creates a default admin user (if not exists)
- **Auto-runs on application startup** (no manual execution needed)

## Database Tables

The schema includes the following tables:

1. **Users** - User accounts (customers, business owners, admins)
2. **Businesses** - Business profiles
3. **BusinessReviews** - Customer reviews and ratings (1-5 stars)
4. **BusinessFollowers** - User-business follow relationships
5. **BusinessPosts** - Business updates and posts
6. **BusinessCategories** - Category tags for businesses
7. **PasswordResetTokens** - Password reset token management
8. **Notifications** - In-app notifications for users
9. **OtpVerifications** - OTP codes for phone verification
10. **ReviewReports** - Abuse reports for reviews

## Usage

### Automatic Initialization (Recommended)

The database tables are **automatically created** when you run the application:

1. Ensure your PostgreSQL server is running
2. Configure the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=linkoja_db;Username=postgres;Password=yourpassword"
  }
}
```
3. Run the application:
```bash
dotnet run
```

The `DatabaseInitializer` service will:
- Read the `CreateTables.sql` script
- Execute it on startup
- Create tables only if they don't already exist
- Log the initialization process

**Note**: The auto-initialization is idempotent - running it multiple times is safe and will not drop existing data.

### Manual Execution (Alternative)

If you prefer to run the script manually:

#### Option 1: Using psql command line
```bash
psql -U postgres -d linkoja_db -f Database/CreateTables.sql
```

#### Option 2: Using psql interactive mode
```bash
psql -U postgres -d linkoja_db
\i Database/CreateTables.sql
```

#### Option 3: Using pgAdmin
1. Open pgAdmin
2. Connect to your PostgreSQL server
3. Right-click on your database (linkoja_db)
4. Select "Query Tool"
5. Open the `CreateTables.sql` file
6. Execute the script

### Connection String
Update your `appsettings.json` with your database connection:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=linkoja_db;Username=postgres;Password=yourpassword"
  }
}
```

## Default Admin User

The script creates a default admin user:
- **Email**: admin@linkoja.com
- **Password**: Admin123! (Change this immediately in production!)
- **Role**: admin

## Entity Framework Core Migrations (Alternative)

If you prefer using EF Core migrations instead of running SQL directly:

```bash
# Add migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

## Schema Details

### Key Features
- **Foreign Keys**: Proper relationships between tables with CASCADE delete
- **Indexes**: Optimized queries on frequently searched columns
- **Constraints**: 
  - UNIQUE constraints prevent duplicate reviews and follows
  - CHECK constraints enforce rating values (1-5)
  - NOT NULL constraints on required fields
- **Default Values**: Sensible defaults for status fields and timestamps

### Relationships
- Users → Businesses (One-to-Many)
- Businesses → Reviews (One-to-Many)
- Businesses → Followers (Many-to-Many via junction table)
- Businesses → Posts (One-to-Many)
- Users → Notifications (One-to-Many)
- Reviews → Reports (One-to-Many)

## Maintenance

### Cleanup Old Data
```sql
-- Delete expired OTP codes
DELETE FROM "OtpVerifications" WHERE "ExpiresAt" < NOW() - INTERVAL '1 day';

-- Delete used password reset tokens
DELETE FROM "PasswordResetTokens" WHERE "IsUsed" = TRUE AND "CreatedAt" < NOW() - INTERVAL '30 days';

-- Archive old notifications (optional)
DELETE FROM "Notifications" WHERE "IsRead" = TRUE AND "CreatedAt" < NOW() - INTERVAL '90 days';
```

### Backup Database
```bash
pg_dump -U postgres -d linkoja_db -F c -b -v -f linkoja_backup_$(date +%Y%m%d).backup
```

### Restore Database
```bash
pg_restore -U postgres -d linkoja_db -v linkoja_backup_YYYYMMDD.backup
```

## Security Notes

1. **Change Default Admin Password**: The default admin password should be changed immediately after first login
2. **Environment Variables**: Store database credentials in environment variables, not in source code
3. **Limited Permissions**: Create a dedicated database user with minimal required permissions
4. **Regular Backups**: Schedule automated database backups
5. **SSL Connection**: Use SSL for database connections in production

## Performance Tips

1. **Regular VACUUM**: Run VACUUM ANALYZE regularly to maintain statistics
2. **Monitor Indexes**: Check index usage with `pg_stat_user_indexes`
3. **Connection Pooling**: Configure appropriate connection pool size in your application
4. **Partitioning**: Consider table partitioning for high-volume tables (Notifications, OtpVerifications)

## Support

For issues or questions about the database schema, please refer to:
- Entity Framework Core documentation
- PostgreSQL documentation
- Project README.md
