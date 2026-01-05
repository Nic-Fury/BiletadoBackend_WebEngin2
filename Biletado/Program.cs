using Biletado.Contexts;
using Biletado.Repository;
using Biletado.Services;
using Biletado.Domain;
using Biletado.DTOs;
using Biletado.Controllers;

using Microsoft.EntityFrameworkCore;
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