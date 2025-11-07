using System;
using Npgsql;

namespace LinkojaMicroservice.Utilities
{
    public static class EnvVarMapper
    {
        // Map platform-provided env vars into configuration-friendly environment variables
        // (double-underscore -> hierarchical config keys).
        public static void Map()
        {
            try
            {
                // 1) DATABASE_URL -> ConnectionStrings__DefaultConnection
                var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
                var existingConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
                if (!string.IsNullOrEmpty(databaseUrl) && string.IsNullOrEmpty(existingConn))
                {
                    var conn = DatabaseUrlConverter.ConvertPostgresUrlToConnectionString(databaseUrl);
                    if (!string.IsNullOrEmpty(conn))
                    {
                        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", conn);
                    }
                }

                // 2) If a separate DB password is provided, override the password in the connection string
                var dbPassword = Environment.GetEnvironmentVariable("YOUR_DB_PASSWORD_HERE");
                if (!string.IsNullOrEmpty(dbPassword))
                {
                    var cs = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
                    if (!string.IsNullOrEmpty(cs))
                    {
                        try
                        {
                            var builder = new NpgsqlConnectionStringBuilder(cs)
                            {
                                Password = dbPassword
                            };
                            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", builder.ToString());
                        }
                        catch
                        {
                            // ignore parsing failures; do not block startup
                        }
                    }
                }

                // 3) Map other app secrets to hierarchical config keys so builder.Configuration["X:Y"] works
                CopyIfPresent("YOUR_JWT_SECRET_KEY_HERE_MINIM", "Jwt__Secret");
                CopyIfPresent("YOUR_EMAIL", "Email__Address");
                CopyIfPresent("YOUR_EMAIL_PASSWORD_HERE", "Email__Password");
                CopyIfPresent("YOUR_TERMII_API_KEY_HERE", "Termii__ApiKey");
                CopyIfPresent("YOUR_GOOGLE_CLIENT_ID_HERE", "Authentication__Google__ClientId");
                CopyIfPresent("YOUR_GOOGLE_CLIENT_SECRET_HERE", "Authentication__Google__ClientSecret");

                // Note: ASPNETCORE_ENVIRONMENT and ASPNETCORE_URLS are already direct env vars the host uses.
            }
            catch
            {
                // Swallow exceptions to avoid breaking startup if mapping fails.
            }
        }

        private static void CopyIfPresent(string sourceKey, string destKey)
        {
            var val = Environment.GetEnvironmentVariable(sourceKey);
            if (!string.IsNullOrEmpty(val) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(destKey)))
            {
                Environment.SetEnvironmentVariable(destKey, val);
            }
        }
    }
}