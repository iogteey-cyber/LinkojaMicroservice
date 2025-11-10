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
                //1) DATABASE_URL -> ConnectionStrings__DefaultConnection
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

                //2) If a separate DB password is provided, override the password in the connection string
                var dbPassword =
                    Environment.GetEnvironmentVariable("DB_PASSWORD") ??
                    Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ??
                    Environment.GetEnvironmentVariable("PGPASSWORD");
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

                //3) Map other app secrets to hierarchical config keys so builder.Configuration["X:Y"] works
                // NOTE: Host.CreateDefaultBuilder already reads env vars, so you can set these directly as
                // Jwt__Key, SmtpSettings__Password, TermiiSettings__ApiKey, GoogleOAuthSettings__ClientId, etc.
                // The mappings below are only to support alternative env var names used by some platforms.

                // JWT
                CopyIfPresent("JWT_SECRET", "Jwt__Key");
                CopyIfPresent("JWT__KEY", "Jwt__Key");

                // SMTP (from commonly used prefixes)
                CopyIfPresent("SMTP__HOST", "SmtpSettings__Host");
                CopyIfPresent("SMTP__PORT", "SmtpSettings__Port");
                CopyIfPresent("SMTP__ENABLESSL", "SmtpSettings__EnableSsl");
                CopyIfPresent("SMTP__FROMEMAIL", "SmtpSettings__FromEmail");
                CopyIfPresent("SMTP__FROMNAME", "SmtpSettings__FromName");
                CopyIfPresent("SMTP__USERNAME", "SmtpSettings__Username");
                CopyIfPresent("SMTP__PASSWORD", "SmtpSettings__Password");

                // Termii
                CopyIfPresent("TERMII__APIKEY", "TermiiSettings__ApiKey");
                CopyIfPresent("TERMII__SENDERID", "TermiiSettings__SenderId");
                CopyIfPresent("TERMII__APIURL", "TermiiSettings__ApiUrl");
                CopyIfPresent("TERMII__CHANNEL", "TermiiSettings__Channel");

                // Google OAuth
                CopyIfPresent("GOOGLEOAUTH__CLIENTID", "GoogleOAuthSettings__ClientId");
                CopyIfPresent("GOOGLEOAUTH__CLIENTSECRET", "GoogleOAuthSettings__ClientSecret");

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