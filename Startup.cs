using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using LinkojaMicroservice.Data;
using LinkojaMicroservice.Models;

namespace LinkojaMicroservice
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // DbContext setup for PostgreSQL
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

            // Configure SMTP settings
            services.Configure<SmtpSettings>(Configuration.GetSection("SmtpSettings"));
            
            // Configure Termii SMS settings
            services.Configure<TermiiSettings>(Configuration.GetSection("TermiiSettings"));
            
            // Configure Google OAuth settings
            services.Configure<GoogleOAuthSettings>(Configuration.GetSection("GoogleOAuthSettings"));

            // Register HttpClient for external API calls
            services.AddHttpClient<LinkojaMicroservice.Services.TermiiSmsService>();
            services.AddHttpClient<LinkojaMicroservice.Services.GoogleOAuthService>();

            // Register services
            services.AddScoped<LinkojaMicroservice.Services.IAuthService, LinkojaMicroservice.Services.AuthService>();
            services.AddScoped<LinkojaMicroservice.Services.IBusinessService, LinkojaMicroservice.Services.BusinessService>();
            services.AddScoped<LinkojaMicroservice.Services.INotificationService, LinkojaMicroservice.Services.NotificationService>();
            services.AddScoped<LinkojaMicroservice.Services.IOtpService, LinkojaMicroservice.Services.OtpService>();
            services.AddScoped<LinkojaMicroservice.Services.IEmailService, LinkojaMicroservice.Services.EmailService>();
            services.AddScoped<LinkojaMicroservice.Services.ISmsService, LinkojaMicroservice.Services.TermiiSmsService>();
            services.AddScoped<LinkojaMicroservice.Services.IGoogleOAuthService, LinkojaMicroservice.Services.GoogleOAuthService>();
            services.AddScoped<DatabaseInitializer>();

            // JWT authentication
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration["Jwt:Issuer"],
                        ValidAudience = Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
                    };
                });

            // Add controllers
            services.AddControllers();

            // Setup Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "LinkojaMicroservice API", Version = "v1" });
                
                // Add JWT authentication to Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });
                
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DatabaseInitializer dbInitializer)
        {
            // Initialize database on startup (auto-run SQL script)
            dbInitializer.InitializeAsync().GetAwaiter().GetResult();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            // Use authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Enable Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LinkojaMicroservice API v1"));
        }
    }
}