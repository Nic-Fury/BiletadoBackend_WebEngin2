using Biletado.Domain;
using Microsoft.EntityFrameworkCore;

namespace Biletado.Contexts;

public class ReservationsDbContext(DbContextOptions<ReservationsDbContext> options) : DbContext(options)
{
    public DbSet<Reservation> Reservations => Set<Reservation>();
    
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
    }
}
