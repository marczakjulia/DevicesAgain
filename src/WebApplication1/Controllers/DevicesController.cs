
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.DTO;
using WebApplication1.DTOs;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DevicesController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet("mine")]
        [Authorize]
        public async Task<IActionResult> GetMyDevices()
        {

            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();
            var accountWithEmployee = await _context.Account
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Username == username);
            
            if (accountWithEmployee == null || accountWithEmployee.Employee == null)
                return Unauthorized();

            var employeeId = accountWithEmployee.Employee.Id;

 
            var assignedList = await _context.DeviceEmployee
                .Where(de => de.EmployeeId == employeeId)
                .Include(de => de.Device)
                    .ThenInclude(d => d.DeviceType)
                .ToListAsync();

            var result = assignedList
                .Select(de => new DeviceAssignmentDto
                {
                    DeviceEmployeeId     = de.Id,
                    DeviceId             = de.Device.Id,
                    Name                 = de.Device.Name,
                    IsEnabled            = de.Device.IsEnabled,
                    AdditionalProperties = de.Device.AdditionalProperties,
                    DeviceTypeId         = de.Device.DeviceTypeId ?? 0,
                    DeviceTypeName       = de.Device.DeviceType?.Name,
                    IssueDate            = de.IssueDate,
                    ReturnDate           = de.ReturnDate
                })
                .ToList();

            return Ok(result);
        }
        
        [HttpPut("mine/{deviceId:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateMyDevice(int deviceId, [FromBody] UpdateDeviceDto dto)
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var accountWithEmployee = await _context.Account
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Username == username);
            if (accountWithEmployee == null || accountWithEmployee.Employee == null)
                return Unauthorized();

            var employeeId = accountWithEmployee.Employee.Id;
            var assignment = await _context.DeviceEmployee
                .AsNoTracking()
                .FirstOrDefaultAsync(de =>
                    de.DeviceId   == deviceId &&
                    de.EmployeeId == employeeId);

            if (assignment == null)
                return NotFound("Device not found");
            
            var device = await _context.Device
                .FirstOrDefaultAsync(d => d.Id == deviceId);

            if (device == null)
                return NotFound("Device not found");

            if (!string.IsNullOrWhiteSpace(dto.Name))
                device.Name = dto.Name;

            if (dto.IsEnabled.HasValue)
                device.IsEnabled = dto.IsEnabled.Value;

            if (!string.IsNullOrWhiteSpace(dto.AdditionalProperties))
                device.AdditionalProperties = dto.AdditionalProperties!;

            await _context.SaveChangesAsync();
            return NoContent();
        }
        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllDevices(CancellationToken cancellationToken)
        {
            try
            {
                var devices = await _context.Device
                    .Select(d => new DeviceDto(d.Id, d.Name))
                    .ToListAsync(cancellationToken);

                return Ok(devices);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDeviceById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var device = await _context.Device
                    .Include(d => d.DeviceType)
                    .Include(d => d.DeviceEmployees)
                        .ThenInclude(de => de.Employee)
                            .ThenInclude(e => e.Person)
                    .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

                if (device == null)
                    return NotFound($"Device {id} not found");
                
                var additionalJson = JsonDocument.Parse(device.AdditionalProperties ?? "{}")
                                                .RootElement;
                var currentAssignment = device.DeviceEmployees
                    .FirstOrDefault(de => de.ReturnDate == null);

                CurrentUserDTO? currentUser = null;
                if (currentAssignment != null && currentAssignment.Employee?.Person != null)
                {
                    var person = currentAssignment.Employee.Person;
                    currentUser = new CurrentUserDTO
                    {
                        Id = currentAssignment.EmployeeId,
                        Name = $"{person.FirstName} {person.LastName}"
                    };
                }

                var dto = new DeviceDtoById
                {
                    Name = device.Name,
                    DeviceTypeName = device.DeviceType!.Name,
                    IsEnabled = device.IsEnabled,
                    AdditionalProperties = additionalJson,
                    Employee = currentUser
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateDevice(
            [FromBody] CreateDevice dev,
            CancellationToken cancellationToken)
        {
            try
            {
                if (dev.AdditionalProperties.ValueKind == JsonValueKind.Null)
                    return BadRequest("AdditionalProperties cannot be null");
                var type = await _context.DeviceType
                    .SingleOrDefaultAsync(t => t.Name == dev.DeviceTypeName, cancellationToken);

                if (type == null)
                    return BadRequest($"Unknown device type '{dev.DeviceTypeName};");

                var device = new Device
                {
                    Name = dev.Name,
                    DeviceTypeId = type.Id,
                    IsEnabled = dev.IsEnabled,
                    AdditionalProperties = dev.AdditionalProperties.GetRawText()
                };

                _context.Device.Add(device);
                await _context.SaveChangesAsync(cancellationToken);
                var returnedDto = new
                {
                    id = device.Id,
                    name = device.Name,
                    deviceTypeName = type.Name,
                    isEnabled = device.IsEnabled,
                    additionalProperties = JsonDocument
                                            .Parse(device.AdditionalProperties ?? "{}")
                                            .RootElement
                };

                return Created($"/api/devices/{device.Id}", returnedDto);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
        
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateDevice(
            int id,
            [FromBody] CreateDevice dev,
            CancellationToken cancellationToken)
        {
            try
            {
                var device = await _context.Device.FindAsync(new object[] { id }, cancellationToken);
                if (device == null)
                    return NotFound($"Device {id} not found");

                var type = await _context.DeviceType
                    .SingleOrDefaultAsync(t => t.Name == dev.DeviceTypeName, cancellationToken);
                if (type == null)
                    return BadRequest($"Unknown device type '{dev.DeviceTypeName}'");

                device.Name = dev.Name;
                device.DeviceTypeId = type.Id;
                device.IsEnabled = dev.IsEnabled;
                device.AdditionalProperties = dev.AdditionalProperties.GetRawText();

                await _context.SaveChangesAsync(cancellationToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
        
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDevice(int id, CancellationToken cancellationToken)
        {
            try
            {
                var device = await _context.Device.FindAsync(new object[] { id }, cancellationToken);
                if (device == null)
                    return NotFound($"Device {id} not found");

                var isAssigned = await _context.DeviceEmployee
                    .AnyAsync(de => de.DeviceId == id, cancellationToken);
                if (isAssigned)
                    return BadRequest($"Cannot delete device {id} because it is associated with an employee");
                _context.Device.Remove(device);
                await _context.SaveChangesAsync(cancellationToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                return Problem(detail: ex.Message);
            }
        }
    }
}
