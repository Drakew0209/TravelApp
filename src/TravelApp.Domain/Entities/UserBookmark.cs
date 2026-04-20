namespace TravelApp.Domain.Entities;

public class UserBookmark
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public int PoiId { get; set; }
    public DateTimeOffset SavedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
}
