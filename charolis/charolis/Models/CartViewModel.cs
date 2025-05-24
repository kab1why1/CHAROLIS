namespace charolis.Models
{
    public class CartViewModel
    {
        public List<Entities.Order> Orders { get; set; } = new();
        public decimal Balance { get; set; }
    }
}