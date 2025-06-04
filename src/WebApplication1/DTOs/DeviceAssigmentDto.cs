namespace WebApplication1.DTOs;

public class DeviceAssignmentDto
{
    public int DeviceEmployeeId { get; set; }
    public int DeviceId  { get; set; }
    public string Name { get; set; }
    public bool  IsEnabled { get; set; }
    public string AdditionalProperties { get; set; }
    public int DeviceTypeId { get; set; }
    public string DeviceTypeName { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
}