using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

public class Employee
{
    public int Id { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Salary { get; set; }
    
    public int PositionId { get; set; }
    public virtual Position Position { get; set; }
    
    public int PersonId { get; set; }
    public virtual Person Person { get; set; }

    public DateTime HireDate { get; set; }
    
    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
    
    public virtual ICollection<DeviceEmployee> DeviceEmployees { get; set; } = new List<DeviceEmployee>();
}