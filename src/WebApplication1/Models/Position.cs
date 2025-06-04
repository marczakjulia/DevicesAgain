namespace WebApplication1.Models;

public class Position
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int MinExpYears { get; set; }
    
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}