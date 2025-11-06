# DigitalOcean App Platform Deployment Guide

This guide explains how to deploy the LinkojaMicroservice to DigitalOcean App Platform.

## Prerequisites

1. DigitalOcean account
2. GitHub repository connected to DigitalOcean
3. PostgreSQL database (automatically provisioned via app.yaml)

## Deployment Methods

### Method 1: Using DigitalOcean App Platform UI (Recommended)

1. **Connect Your Repository**
   - Go to https://cloud.digitalocean.com/apps
   - Click "Create App"
   - Select "GitHub" as source
   - Choose repository: `iogteey-cyber/LinkojaMicroservice`
   - Select branch: `main`

2. **Detect App Configuration**
   - App Platform will automatically detect the `.do/app.yaml` file
   - Or it will detect the `Dockerfile` for containerized deployment
   - Review the detected configuration

3. **Configure Database**
   - App Platform will provision a PostgreSQL database cluster
   - Database connection string will be automatically injected as: `${linkoja-db.DATABASE_URL}`

4. **Set Environment Variables**
   The following environment variables are automatically set from app.yaml:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `ASPNETCORE_URLS=http://+:8080`
   - `DATABASE_URL` (from managed database)

   **Add these additional environment variables in the App Platform UI:**

   **JWT Settings:**
   - `JwtSettings__SecretKey` = Your secure JWT secret key (min 32 characters)
   - `JwtSettings__Issuer` = https://your-app.ondigitalocean.app
   - `JwtSettings__Audience` = https://your-app.ondigitalocean.app
   - `JwtSettings__ExpirationMinutes` = 60

   **SMTP Settings:**
   - `SmtpSettings__Host` = smtp.office365.com
   - `SmtpSettings__Port` = 587
   - `SmtpSettings__EnableSsl` = true
   - `SmtpSettings__FromEmail` = finpay@fintraksoftware.com
   - `SmtpSettings__FromName` = Linkoja Notifications
   - `SmtpSettings__Username` = finpay@fintraksoftware.com
   - `SmtpSettings__Password` = Fintrak1234

   **Termii SMS Settings:**
   - `TermiiSettings__ApiKey` = TLjDEAmMsYbcAnTDRBXCSPTdikEEGSxMvFYLgWnPutRFhArorpGCtHiCpuPRHd
   - `TermiiSettings__SenderId` = Linkoja
   - `TermiiSettings__ApiUrl` = https://api.ng.termii.com/api/sms/send
   - `TermiiSettings__Channel` = generic

   **Google OAuth Settings:**
   - `GoogleOAuthSettings__ClientId` = 1027186401789-gmlrvq5qq51ffga0nhepf3173ldc20oq.apps.googleusercontent.com
   - `GoogleOAuthSettings__ClientSecret` = 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
   - `GoogleOAuthSettings__TokenValidationUrl` = https://oauth2.googleapis.com/tokeninfo

5. **Deploy**
   - Review all settings
   - Click "Create Resources"
   - Wait for deployment (usually 5-10 minutes)

6. **Verify Deployment**
   - Once deployed, you'll get a URL like: `https://your-app-name.ondigitalocean.app`
   - Access Swagger UI at: `https://your-app-name.ondigitalocean.app/swagger`
   - Check logs for: "Database initialization completed successfully"

### Method 2: Using DigitalOcean CLI (doctl)

```bash
# Install doctl
brew install doctl  # macOS
# or download from: https://docs.digitalocean.com/reference/doctl/how-to/install/

# Authenticate
doctl auth init

# Create app from spec
doctl apps create --spec .do/app.yaml

# Get app ID
doctl apps list

# Update environment variables
doctl apps update YOUR_APP_ID --spec .do/app.yaml

# View logs
doctl apps logs YOUR_APP_ID --type=run
```

### Method 3: Using Dockerfile Directly

If App Platform doesn't detect the configuration:

1. In the App Platform UI, select "Dockerfile" as the build type
2. Dockerfile location: `/Dockerfile`
3. HTTP port: 8080
4. Run command: (leave default, handled by Dockerfile)

## Database Connection

The managed PostgreSQL database connection string is automatically provided as an environment variable:
- Variable: `DATABASE_URL`
- Format: `postgresql://username:password@host:port/database?sslmode=require`

**Update appsettings.json for production:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "${DATABASE_URL}"
  }
}
```

Or set via environment variable:
- `ConnectionStrings__DefaultConnection` = `${linkoja-db.DATABASE_URL}`

## Database Initialization

The DatabaseInitializer service will automatically:
1. Connect to the PostgreSQL database
2. Run the idempotent SQL scripts from `Database/CreateTables.sql`
3. Create all 11 tables if they don't exist
4. Create the default admin user
5. Log success/failure

Check logs for: "Database initialization completed successfully"

## Scaling

**Manual Scaling:**
```bash
doctl apps update YOUR_APP_ID --instance-count 3
```

**Or via UI:**
- Go to your app → Settings → Scale
- Adjust instance count (1-10)
- Choose instance size (basic-xxs to professional-xl)

## Monitoring

**View Logs:**
```bash
# Real-time logs
doctl apps logs YOUR_APP_ID --type=run --follow

# Deployment logs
doctl apps logs YOUR_APP_ID --type=build
```

**Metrics:**
- CPU usage
- Memory usage
- Request rate
- Response time

Available in App Platform dashboard.

## Custom Domain

1. Go to your app → Settings → Domains
2. Add your custom domain (e.g., api.linkoja.com)
3. Update DNS records as instructed
4. SSL certificate is automatically provisioned

## Health Checks

Configured in app.yaml:
- **Path:** `/swagger/index.html`
- **Initial Delay:** 30 seconds
- **Period:** 10 seconds
- **Timeout:** 5 seconds
- **Failure Threshold:** 3 consecutive failures

## Troubleshooting

**Build Failures:**
1. Check build logs: `doctl apps logs YOUR_APP_ID --type=build`
2. Verify .NET 6.0 SDK compatibility
3. Check for missing dependencies

**Runtime Failures:**
1. Check runtime logs: `doctl apps logs YOUR_APP_ID --type=run`
2. Verify environment variables are set correctly
3. Check database connection string
4. Verify database initialization completed

**Database Connection Issues:**
1. Ensure database is in the same region as app
2. Check DATABASE_URL environment variable
3. Verify SSL mode is enabled
4. Check database cluster status

**Port Issues:**
- App must listen on port 8080 (configured in Dockerfile and app.yaml)
- ASPNETCORE_URLS is set to `http://+:8080`

## Cost Estimation

**Basic Setup:**
- App (basic-xxs): $5/month
- Managed PostgreSQL (basic): $15/month
- **Total: ~$20/month**

**Production Setup:**
- App (professional-xs): $12/month (or higher)
- Managed PostgreSQL (basic or standard): $15-60/month
- Additional instances for scaling
- **Total: $27-100+/month**

## Security Recommendations

1. **Use App Platform Secrets** for sensitive data:
   - Database passwords
   - API keys
   - JWT secret keys
   - OAuth credentials

2. **Enable SSL/TLS** (automatic with managed domains)

3. **Restrict Database Access:**
   - Use App Platform's trusted sources
   - Enable SSL mode for PostgreSQL connections

4. **Rotate Credentials Regularly:**
   - JWT secret key
   - Database passwords
   - API keys

5. **Monitor Logs:**
   - Set up log forwarding to external services
   - Monitor for security events

## Continuous Deployment

With the configuration in `.do/app.yaml`, your app will automatically redeploy when you push to the `main` branch.

**Disable auto-deploy:**
```yaml
deploy_on_push: false
```

**Manual deployment:**
```bash
doctl apps create-deployment YOUR_APP_ID
```

## Backup and Recovery

**Database Backups:**
- Automatic daily backups (retained for 7 days)
- Manual backups via UI or CLI
- Point-in-time recovery available

**Create manual backup:**
```bash
doctl databases backup create YOUR_DB_ID
```

## Support

- DigitalOcean Docs: https://docs.digitalocean.com/products/app-platform/
- Community: https://www.digitalocean.com/community/
- Support: https://cloud.digitalocean.com/support

## API Endpoints

After deployment, your API will be available at:
- Base URL: `https://your-app-name.ondigitalocean.app`
- Swagger UI: `https://your-app-name.ondigitalocean.app/swagger`
- Health Check: `https://your-app-name.ondigitalocean.app/swagger/index.html`

All 36 endpoints will be accessible as documented in the main README.md.
