using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.IO;
using System.Threading.Tasks;
using LinkojaMicroservice.Utilities;

namespace LinkojaMicroservice.Data
{
    public class DatabaseInitializer
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(IConfiguration configuration, ILogger<DatabaseInitializer> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Prefer configuration, then environment variables
                var rawConn = _configuration.GetConnectionString("DefaultConnection")
                              ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                              ?? Environment.GetEnvironmentVariable("DATABASE_URL");

                if (string.IsNullOrWhiteSpace(rawConn))
                {
                    _logger.LogWarning("Database connection string not found. Skipping database initialization.");
                    return;
                }

                // Try to convert URL-form DATABASE_URL to an Npgsql-compatible connection string.
                // DatabaseUrlConverter is defensive and returns null for unknown formats.
                var converted = DatabaseUrlConverter.ConvertPostgresUrlToConnectionString(rawConn);

                // Choose converted result if available, otherwise use the trimmed raw value.
                var effectiveConn = (converted ?? rawConn ?? string.Empty).Trim().Trim('"', '\'');

                // Determine a non-sensitive format hint for logging (do not log the connection value itself)
                string formatHint;
                var rr = (rawConn ?? string.Empty).Trim();
                if (rr.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || rr.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
                {
                    formatHint = "url";
                }
                else if (rr.Contains("=") || rr.Contains(";"))
                {
                    formatHint = "connection-string-like";
                }
                else
                {
                    formatHint = "unknown";
                }

                _logger.LogInformation("Database initialization: detected DB env format: {FormatHint}", formatHint);

                // Validate the final connection string by constructing an NpgsqlConnectionStringBuilder
                NpgsqlConnectionStringBuilder csBuilder;
                try
                {
                    csBuilder = new NpgsqlConnectionStringBuilder(effectiveConn);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Invalid DB connection string format ({FormatHint}). Ensure DATABASE_URL is a postgres://... URL or ConnectionStrings__DefaultConnection is a valid key=value connection string.", formatHint);
                    return; // Skip initialization to avoid the low-level Npgsql parsing error
                }

                _logger.LogInformation("Starting database initialization...");

                // Read SQL script
                var sqlScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", "CreateTables.sql");

                if (!File.Exists(sqlScriptPath))
                {
                    _logger.LogWarning("SQL script not found at {Path}. Skipping database initialization.", sqlScriptPath);
                    return;
                }

                var sqlScript = await File.ReadAllTextAsync(sqlScriptPath);

                // Execute SQL script
                await using var connection = new NpgsqlConnection(csBuilder.ToString());
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(sqlScript, connection);
                command.CommandTimeout = 300; // 5 minutes timeout for large scripts

                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during database initialization: {Message}", ex.Message);
                // Don't throw - allow application to continue even if initialization fails
                // This is useful in development where the database might not be set up yet
            }
        }
    }
}