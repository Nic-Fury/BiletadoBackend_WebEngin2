using Biletado.Contexts;
using Biletado.Repository;
using Biletado.Services;
using Biletado.Domain;
using Biletado.DTOs;
using Biletado.Controllers;

using Microsoft.EntityFrameworkCore;

namespace Biletado;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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
        builder.Services.AddScoped<ReservationStatusService>();
        
        // Configure Swagger/OpenAPI
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}