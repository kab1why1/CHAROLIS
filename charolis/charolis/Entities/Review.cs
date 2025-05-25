namespace charolis.Entities;

public class Review : BaseEntity
{
    public override int Id { get; set; }

    // Зв'язок з товаром
    public int ProductId { get; set; }
    public Product Product { get; set; }

    // Зв'язок з користувачем
    public int UserId { get; set; }
    public User User { get; set; }

    public int Rating { get; set; }        // від 1 до 5
    public string Comment { get; set; }    // текст відгуку
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}