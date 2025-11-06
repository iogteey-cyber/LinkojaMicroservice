# Configuration Management

## Overview

This document explains how to manage application secrets and configuration values securely.

## Local Development Setup

### Step 1: Create Local Configuration File

Copy the example file and add your actual credentials:

```bash
cp appsettings.Local.json.example appsettings.Local.json
```

The `appsettings.Local.json` file is **already in .gitignore** and will never be committed.

### Step 2: Configure Your Credentials

Edit `appsettings.Local.json` with your actual values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=LinkojaDb;Username=postgres;Password=YOUR_ACTUAL_PASSWORD"
  },
  "Jwt": {
    "Key": "YOUR_ACTUAL_JWT_SECRET_KEY_MINIMUM_32_CHARS"
  },
  "SmtpSettings": {
    "FromEmail": "your-email@domain.com",
    "Username": "your-email@domain.com",
    "Password": "YOUR_ACTUAL_EMAIL_PASSWORD"
  },
  "TermiiSettings": {
    "ApiKey": "YOUR_ACTUAL_TERMII_API_KEY"
  },
  "GoogleOAuthSettings": {
    "ClientId": "YOUR_ACTUAL_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_ACTUAL_GOOGLE_CLIENT_SECRET"
  }
}
```

### Configuration Values from Issue

Based on the project requirements, here are the actual values to use:

**Database:**
- Password: `sqluser10$`

**JWT:**
- Key: `455665544weerQQQe5566yyhhggyy@@##$$3ffgghhhhhjjjj!`

**SMTP (Office 365):**
- FromEmail: `finpay@fintraksoftware.com`
- Username: `finpay@fintraksoftware.com`
- Password: `Fintrak@1234`

**Termii SMS Gateway:**
- ApiKey: `TLjDEAmMsYbcAnTDRBXCSPTdikEEGSxMvFYLgWnPutRFhArorpGCtHiCpuPRHd`

**Google OAuth:**
- ClientId: `1027186401789-gmlrvq5qq51ffga0nhepf3173ldc20oq.apps.googleusercontent.com`
- ClientSecret: `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`

## Configuration Loading Order

ASP.NET Core loads configuration in this order (later sources override earlier ones):

1. `appsettings.json` (template with placeholders)
2. `appsettings.{Environment}.json` (e.g., Development, Production)
3. `appsettings.Local.json` (your local secrets - not committed)
4. Environment variables
5. Command-line arguments

## Environment Variables (Production)

For production deployments (DigitalOcean, Azure, AWS), set these environment variables:

### Database
```bash
ConnectionStrings__DefaultConnection="Host=your-db-host;Port=5432;Database=LinkojaDb;Username=postgres;Password=your-password"
```

### JWT
```bash
Jwt__Key="your-production-jwt-secret-key-min-32-chars"
Jwt__Issuer="LinkojaMicroservice"
Jwt__Audience="LinkojaApp"
```

### SMTP Settings
```bash
SmtpSettings__Host="smtp.office365.com"
SmtpSettings__Port="587"
SmtpSettings__EnableSsl="true"
SmtpSettings__FromEmail="your-email@domain.com"
SmtpSettings__FromName="Linkoja Notifications"
SmtpSettings__Username="your-email@domain.com"
SmtpSettings__Password="your-email-password"
```

### Termii SMS
```bash
TermiiSettings__ApiKey="your-termii-api-key"
TermiiSettings__SenderId="Linkoja"
```

### Google OAuth
```bash
GoogleOAuthSettings__ClientId="your-google-client-id"
GoogleOAuthSettings__ClientSecret="your-google-client-secret"
```

## DigitalOcean App Platform

When deploying to DigitalOcean, add environment variables via the App Platform UI:

1. Go to your app's Settings → App-Level Environment Variables
2. Add each variable listed above
3. The database connection string is automatically provided by the platform

See `DEPLOYMENT.md` for complete deployment instructions.

## Security Best Practices

1. ✅ **Never commit** `appsettings.Local.json` or `appsettings.Production.json`
2. ✅ **Always use** environment variables for production secrets
3. ✅ **Rotate credentials** regularly (every 90 days recommended)
4. ✅ **Use different secrets** for each environment (dev, staging, production)
5. ✅ **Audit access** to secrets regularly
6. ✅ **Consider** using Azure Key Vault or AWS Secrets Manager for production

## Troubleshooting

### "Configuration value not found"
- Check if `appsettings.Local.json` exists and has the correct values
- Verify environment variables are set correctly
- Check spelling and casing (configuration keys are case-sensitive)

### "Database connection failed"
- Verify PostgreSQL is running
- Check connection string in `appsettings.Local.json`
- Ensure database exists: `createdb LinkojaDb`

### "JWT token validation failed"
- Ensure JWT:Key is at least 32 characters
- Verify the same key is used by both API and clients

## Files

- `appsettings.json` - Template with placeholders (committed to git)
- `appsettings.Development.json` - Development overrides (committed to git)
- `appsettings.Local.json.example` - Example with actual values (committed to git)
- `appsettings.Local.json` - Your local secrets (**NOT committed to git**)
- `appsettings.Production.json` - Production secrets (**NOT committed to git**)

## Quick Start

```bash
# 1. Copy example to local config
cp appsettings.Local.json.example appsettings.Local.json

# 2. Edit with your actual values
nano appsettings.Local.json

# 3. Run the application
dotnet run

# The app will automatically load settings from appsettings.Local.json
```
