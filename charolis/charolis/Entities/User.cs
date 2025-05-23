namespace charolis.Entities;

public abstract class BaseEntity
{
    public abstract int Id { get; set; }
    public void GetInfo() => Id = 10;
    public virtual void SetInfo(int newId) => Id = newId;
}

public class User : BaseEntity
{
    public override int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string Role { get; set; } = "User";

    public override void SetInfo(int newId)
    {
        base.SetInfo(newId);
        Console.WriteLine(newId);
    }
}

public class Admin : User
{
    public string AdminUsername { get; private set; }
    public string AdminPassword { get; private set; }
    
}