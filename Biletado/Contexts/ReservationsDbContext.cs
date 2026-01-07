using Biletado.Domain;
using Microsoft.EntityFrameworkCore;

namespace Biletado.Contexts;

public class ReservationsDbContext : DbContext
{
    private readonly ILogger<ReservationsDbContext>? _logger;
    
    public ReservationsDbContext(DbContextOptions<ReservationsDbContext> options, ILogger<ReservationsDbContext>? logger = null) : base(options)
    {
        _logger = logger;
    }
    
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<User> Users => Set<User>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<Reservation>(r =>
        {
            r.ToTable("reservations");
            r.HasKey(x => x.Id);
            r.Property(x => x.Id).HasColumnName("id");
            r.Property(x => x.From).HasColumnName("from").IsRequired();
            r.Property(x => x.To).HasColumnName("to").IsRequired();
            r.Property(x => x.RoomId).HasColumnName("room_id").IsRequired();
            r.Property(x => x.DeletedAt).HasColumnName("deleted_at");
            r.HasQueryFilter(x => x.DeletedAt == null); // soft-delete filter
            r.HasIndex(x => new { x.RoomId, x.From, x.To })
                .HasDatabaseName("ix_reservations_room_id_start_time_end_time");
        });

        modelBuilder.Entity<User>(u =>
        {
            u.ToTable("users");
            u.HasKey(x => x.Id);
            u.Property(x => x.Id).HasColumnName("id");
            u.Property(x => x.Username).HasColumnName("username").IsRequired().HasMaxLength(100);
            u.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            u.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            u.HasIndex(x => x.Username).IsUnique().HasDatabaseName("ix_users_username");
        });
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("Saving changes to database");
        try
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            _logger?.LogInformation("Database changes saved: {ChangeCount} entities affected", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save changes to database");
            throw;
        }
    }
}
