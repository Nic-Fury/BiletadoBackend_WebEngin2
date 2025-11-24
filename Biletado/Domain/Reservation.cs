namespace Biletado.Domain;

public class Reservation
{
    public Guid Id { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    
    public DateOnly From { get; set; }
    
    public DateOnly To { get; set; }
    
    public Guid RoomId { get; set; }
    
    public bool IsDeleted => DeletedAt != null;

}