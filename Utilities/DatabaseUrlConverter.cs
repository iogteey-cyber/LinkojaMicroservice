using System;
using Npgsql;

namespace LinkojaMicroservice.Utilities
{
    public static class DatabaseUrlConverter
    {
        // Converts a DATABASE_URL in the form postgres://user:pass@host:port/dbname
        // into an Npgsql-compatible connection string. This method is defensive and
        // accepts both URL and connection-string forms. It trims quotes and whitespace
        // and will return null on unrecognized formats.
        public static string ConvertPostgresUrlToConnectionString(string databaseUrl)
        {
            if (string.IsNullOrWhiteSpace(databaseUrl)) return null;

            // Trim whitespace and surrounding quotes people sometimes add
            databaseUrl = databaseUrl.Trim().Trim('"', '\'');

            // If it already looks like a connection string (contains '=' or semicolons), accept it
            if (databaseUrl.Contains("=") && databaseUrl.Contains(";"))
            {
                return databaseUrl;
            }

            // Some platforms may provide Host=... without semicolons
            if (databaseUrl.StartsWith("Host=", StringComparison.OrdinalIgnoreCase) ||
                databaseUrl.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
            {
                return databaseUrl;
            }

            // Handle URL form
            if (databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var uri = new Uri(databaseUrl);
                    var userInfo = uri.UserInfo.Split(new[] { ':', }, 2);

                    var builder = new NpgsqlConnectionStringBuilder
                    {
                        Host = uri.Host,
                        Port = uri.Port > 0 ? uri.Port : 5432,
                        Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty,
                        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
                        Database = uri.AbsolutePath.TrimStart('/'),
                        SslMode = SslMode.Require,
                        TrustServerCertificate = true
                    };

                    return builder.ToString();
                }
                catch
                {
                    return null;
                }
            }

            // Unknown format
            return null;
        }
    }
}