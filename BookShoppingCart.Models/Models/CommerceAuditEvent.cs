namespace BookShoppingCart.Models.Models;
public sealed class CommerceAuditEvent
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = "";
    public int EntityId { get; set; }
    public string ActorId { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public string Before { get; set; } = "";
    public string After { get; set; } = "";
    public string Reason { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
