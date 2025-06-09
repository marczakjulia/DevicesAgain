using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.DTO;
using WebApplication1.DTOs;
using WebApplication1.Models;
using Microsoft.Extensions.Logging;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/devices")]
    public class DevicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(ApplicationDbContext context, ILogger<DevicesController> logger)
        {
            _context = context;
            _logger = logger;
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllDevices(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting all devices");
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
        [HttpGet("types")]
        public async Task<IActionResult> GetAllDevicesTypes(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting all devices types");
                var devices = await _context.DeviceType
                    .Select(d => new DeviceTypeDto(d.Id, d.Name))
                    .ToListAsync(cancellationToken);

                return Ok(devices);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetDeviceById(int id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting device by id");
            var isAdmin = User.IsInRole("Admin");
            int? employeeId = null;
            if (!isAdmin)
            {
                var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Forbid();
                var account = await _context.Account.Include(a => a.Employee)
                    .FirstOrDefaultAsync(a => a.Username == username, cancellationToken);
                if (account == null || account.Employee == null)
                    return Forbid();
                employeeId = account.Employee.Id;
                var assigned = await _context.DeviceEmployee.AnyAsync(de => de.DeviceId == id && de.EmployeeId == employeeId && de.ReturnDate == null, cancellationToken);
                if (!assigned)
                    return Forbid();
            }
            try
            {
                var device = await _context.Device
                    .Include(d => d.DeviceType)
                    .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

                if (device == null)
                {
                    _logger.LogWarning("Device {id} not found", id);
                    return NotFound($"Device {id} not found");
                }
                var additionalJson = JsonDocument.Parse(device.AdditionalProperties ?? "{}").RootElement;
                var result = new
                {
                    name = device.Name,
                    isEnabled = device.IsEnabled,
                    additionalProperties = additionalJson,
                    type = device.DeviceType?.Name
                };
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in GetDeviceById");
                return Problem(ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateDevice([FromBody] CreateDevice dev, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Creating new device");
                if (dev.AdditionalProperties.ValueKind == JsonValueKind.Null)
                    return BadRequest("AdditionalProperties cannot be null");
                var type = await _context.DeviceType
                    .SingleOrDefaultAsync(t => t.Id == dev.TypeId, cancellationToken);

                if (type == null)
                    return BadRequest($"Unknown device type id '{dev.TypeId}'");

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
                    isEnabled = device.IsEnabled,
                    additionalProperties = JsonDocument
                                            .Parse(device.AdditionalProperties ?? "{}")
                                            .RootElement,
                    typeId = type.Id
                };
                _logger.LogInformation("Created device");

                return Created($"/api/devices/{device.Id}", returnedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in CreateDevice");
                return Problem(ex.Message);
            }
        }
        
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateDevice(int id, [FromBody] CreateDevice dev, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating device");
            var isAdmin = User.IsInRole("Admin");
            int? employeeId = null;
            if (!isAdmin)
            {
                var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Forbid();
                var account = await _context.Account.Include(a => a.Employee)
                    .FirstOrDefaultAsync(a => a.Username == username, cancellationToken);
                if (account == null || account.Employee == null)
                    return Forbid();
                employeeId = account.Employee.Id;
                var assigned = await _context.DeviceEmployee.AnyAsync(de => de.DeviceId == id && de.EmployeeId == employeeId && de.ReturnDate == null, cancellationToken);
                if (!assigned)
                    return Forbid();
            }
            try
            {
                var device = await _context.Device.FindAsync(new object[] { id }, cancellationToken);
                if (device == null)
                    return NotFound($"Device {id} not found");

                var type = await _context.DeviceType
                    .SingleOrDefaultAsync(t => t.Id == dev.TypeId, cancellationToken);
                if (type == null)
                    return BadRequest($"Unknown device type id '{dev.TypeId}'");

                device.Name = dev.Name;
                device.DeviceTypeId = type.Id;
                device.IsEnabled = dev.IsEnabled;
                device.AdditionalProperties = dev.AdditionalProperties.GetRawText();

                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated device");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in UpdateDevice");
                return Problem(ex.Message);
            }
        }
        
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDevice(int id, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Deleting device");
                var device = await _context.Device.FindAsync(new object[] { id }, cancellationToken);
                if (device == null)
                    return NotFound($"Device {id} not found");

                var isAssigned = await _context.DeviceEmployee
                    .AnyAsync(de => de.DeviceId == id, cancellationToken);
                if (isAssigned)
                    return BadRequest($"Cannot delete device {id} because it is associated with an employee");
                _context.Device.Remove(device);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Deleted device");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in DeleteDevice");
                return Problem(detail: ex.Message);
            }
        }

        
    }
}
