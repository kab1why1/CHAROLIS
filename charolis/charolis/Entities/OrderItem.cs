namespace charolis.Entities;

public class OrderItem : BaseEntity
{
    public override int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    
    public int ProductId { get; set; }
    public Product Product { get; set; }
    
    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }
}