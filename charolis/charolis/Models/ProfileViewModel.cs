namespace charolis.Models;

public class ProfileViewModel
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public decimal Balance { get; set; }

    // Optional: change password
    public string NewPassword { get; set; }
    public string ConfirmPassword { get; set; }
}