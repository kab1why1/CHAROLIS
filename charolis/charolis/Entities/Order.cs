namespace charolis.Entities;

public class Order : BaseEntity
{
    public override int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public decimal Total { get; set; }
    
    public bool IsPaid { get; set; } = false;
    public DateTime? PaidAt { get; set; }
    
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}