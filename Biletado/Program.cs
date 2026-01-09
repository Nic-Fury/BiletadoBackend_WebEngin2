using Biletado.Contexts;
using Biletado.Repository;
using Biletado.Services;
using Biletado.Domain;
using Biletado.DTOs;
using Biletado.Controllers;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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

            // Configure JWT Authentication
            var jwtSettings = builder.Configuration.GetSection("Jwt");
            var authority = jwtSettings["Authority"];
            var audience = jwtSettings["Audience"];

            if (string.IsNullOrEmpty(authority))
            {
                Log.Fatal("JWT Authority configuration is missing or empty");
                throw new InvalidOperationException("JWT Authority must be configured in appsettings.json");
            }

            if (string.IsNullOrEmpty(audience))
            {
                Log.Fatal("JWT Audience configuration is missing or empty");
                throw new InvalidOperationException("JWT Audience must be configured in appsettings.json");
            }

            Log.Information("Configuring JWT authentication: Authority={Authority}, Audience={Audience}", authority, audience);

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authority;
                    options.Audience = audience;
                    options.RequireHttpsMetadata = jwtSettings.GetValue<bool>("RequireHttpsMetadata");
                    
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = jwtSettings.GetValue<bool>("ValidateIssuer"),
                        ValidateAudience = jwtSettings.GetValue<bool>("ValidateAudience"),
                        ValidateLifetime = jwtSettings.GetValue<bool>("ValidateLifetime"),
                        ValidateIssuerSigningKey = jwtSettings.GetValue<bool>("ValidateIssuerSigningKey"),
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Log.Warning("JWT authentication failed: {Error}", context.Exception.Message);
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            Log.Debug("JWT token validated for user: {User}", context.Principal?.Identity?.Name ?? "Unknown");
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            // Register Repository and Services
            builder.Services.AddScoped<ReservationServiceRepository>();
            builder.Services.AddScoped<IReservationStatusService, ReservationStatusService>();
            builder.Services.AddScoped<IReservationService, ReservationStatusService>();
            builder.Services.AddScoped<ReservationStatusService>();
            
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