namespace charolis.Models;

public class ReviewViewModel
{
    public int ProductId { get; set; }
    public int Id { get; set; } // для редагування
    public int Rating { get; set; }
    public string Comment { get; set; }
}