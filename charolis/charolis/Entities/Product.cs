namespace charolis.Entities;

public class Product : BaseEntity
{
    override public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}