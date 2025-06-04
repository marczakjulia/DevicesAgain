namespace WebApplication1.Models;

public class DeviceEmployee
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public virtual Device Device { get; set; }
    public int EmployeeId { get; set; }
    public virtual Employee Employee { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
}