using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.IO;
using System.Threading.Tasks;

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
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogWarning("Database connection string not found. Skipping database initialization.");
                    return;
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
                await using var connection = new NpgsqlConnection(connectionString);
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
