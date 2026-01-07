using Biletado.Contexts;
using Biletado.Repository;
using Biletado.Services;
using Biletado.Domain;
using Biletado.DTOs;
using Biletado.Controllers;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Serilog.Events;

namespace Biletado;

public class Program
{
    public static void Main(string[] args)
    {
        // Configure Serilog early to capture startup errors
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting Biletado Reservations API");

            var builder = WebApplication.CreateBuilder(args);

            // Configure Serilog from appsettings.json
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext());

            // Add services to the container
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = 
                        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                });

            // Configure Database
            builder.Services.AddDbContext<ReservationsDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("ReservationsDb")));

            // Register Repository and Services
            builder.Services.AddScoped<ReservationServiceRepository>();
            builder.Services.AddScoped<IReservationStatusService, ReservationStatusService>();
            builder.Services.AddScoped<IReservationService, ReservationStatusService>();
            builder.Services.AddScoped<ReservationStatusService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            
            // Configure JWT Authentication
            var jwtKey = builder.Configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException("JWT Key is not configured. Please set 'Jwt:Key' in appsettings.json");
            }

            // Validate JWT key length (HMAC-SHA256 requires at least 256 bits / 32 bytes)
            if (Encoding.UTF8.GetBytes(jwtKey).Length < 32)
            {
                throw new InvalidOperationException("JWT Key must be at least 32 characters long for security. Current length: " + Encoding.UTF8.GetBytes(jwtKey).Length);
            }

            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            if (string.IsNullOrWhiteSpace(jwtIssuer))
            {
                throw new InvalidOperationException("JWT Issuer is not configured. Please set 'Jwt:Issuer' in appsettings.json");
            }

            var jwtAudience = builder.Configuration["Jwt:Audience"];
            if (string.IsNullOrWhiteSpace(jwtAudience))
            {
                throw new InvalidOperationException("JWT Audience is not configured. Please set 'Jwt:Audience' in appsettings.json");
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

            builder.Services.AddAuthorization();
            
            // Configure Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Add Serilog request logging
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                    diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
                    diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
                };
            });

            // Configure the HTTP request pipeline ;) (;
            if (app.Environment.IsDevelopment())
            {
                Log.Information("Running in Development mode - Swagger UI enabled");
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            Log.Information("Biletado Reservations API started successfully");
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.Information("Shutting down Biletado Reservations API");
            Log.CloseAndFlush();
        }
    }
}