using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using LinkojaMicroservice;
using Serilog;
using System;
using Microsoft.Extensions.Configuration;
using LinkojaMicroservice.Utilities;

public class Program
{
    public static void Main(string[] args)
    {
        // Map platform env vars into config-friendly env vars before configuration is read
        EnvVarMapper.Map();

        // If DIGITALOCEAN (App Platform) provides DATABASE_URL (postgres://user:pass@host:port/db)
        // convert it to an Npgsql-compatible connection string and set it as
        // the ConnectionStrings__DefaultConnection environment variable so EF Core can use
        // Configuration.GetConnectionString("DefaultConnection").
        try
        {
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            var existingConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (!string.IsNullOrEmpty(databaseUrl) && string.IsNullOrEmpty(existingConn))
            {
                var conn = LinkojaMicroservice.Utilities.DatabaseUrlConverter.ConvertPostgresUrlToConnectionString(databaseUrl);
                if (!string.IsNullOrEmpty(conn))
                {
                    Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", conn);
                }
            }
        }
        catch (Exception ex)
        {
            // don't let env parsing break startup - log to Console (Serilog configured below will pick up if available)
            Console.WriteLine($"Warning parsing DATABASE_URL: {ex.Message}");
        }

        // Configure Serilog to read from configuration including environment variables
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build())
            .CreateLogger();

        try
        {
            Log.Information("Starting Linkoja Microservice");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application start-up failed");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog() // Use Serilog instead of default logging
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}