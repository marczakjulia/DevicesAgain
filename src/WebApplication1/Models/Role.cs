namespace WebApplication1.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; }
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}