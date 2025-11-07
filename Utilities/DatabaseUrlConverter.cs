using System;
using Npgsql;

namespace LinkojaMicroservice.Utilities
{
    public static class DatabaseUrlConverter
    {
        // Converts a DATABASE_URL in the form postgres://user:pass@host:port/dbname
        // into an Npgsql-compatible connection string.
        public static string ConvertPostgresUrlToConnectionString(string databaseUrl)
        {
            if (string.IsNullOrWhiteSpace(databaseUrl)) return null;
            try
            {
                var uri = new Uri(databaseUrl);
                var userInfo = uri.UserInfo.Split(':', 2);

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
    }
}