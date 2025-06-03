using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.DTOs;
using WebApplication1.Models;

namespace WebApplication1;

// DevicesController for device endpoints
[Route("api/devices")]
[ApiController]
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
                return Unauthorized(new { message = "Token is missing the NameIdentifier claim." });
            var accountWithEmployee = await _context.Account
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Username == username);

            if (accountWithEmployee == null)
                return NotFound("Account not found.");

            if (accountWithEmployee.Employee == null)
                return NotFound("Employee record not found for this account.");

            var employeeId = accountWithEmployee.Employee.Id;
            
            var assignedList = await _context.DeviceEmployee
                .Where(de => de.EmployeeId == employeeId)
                .Include(de => de.Device)
                    .ThenInclude(d => d.DeviceType)
                .ToListAsync();
            
            var result = assignedList.Select(de => new DeviceAssignmentDto
            {
                DeviceEmployeeId = de.Id,
                DeviceId = de.Device.Id,
                Name = de.Device.Name,
                IsEnabled = de.Device.IsEnabled,
                AdditionalProperties = de.Device.AdditionalProperties,
                DeviceTypeId = de.Device.DeviceTypeId ?? 0,
                DeviceTypeName = de.Device.DeviceType?.Name,
                IssueDate = de.IssueDate,
                ReturnDate = de.ReturnDate
            })
            .ToList();

            return Ok(result);
        }
         
           [HttpPut("mine/{int}")]
        [Authorize]
        public async Task<IActionResult> UpdateMyDevice(int deviceId, UpdateDeviceDto dto)
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { message = "Token is missing the NameIdentifier claim." });
            var accountWithEmployee = await _context.Account
                .Include(a => a.Employee)
                .FirstOrDefaultAsync(a => a.Username == username);

            if (accountWithEmployee == null)
                return NotFound("Account not found.");

            if (accountWithEmployee.Employee == null)
                return NotFound("Employee record not found for this account.");

            var employeeId = accountWithEmployee.Employee.Id;
            var assignment = await _context.DeviceEmployee
                .AsNoTracking() 
                .FirstOrDefaultAsync(de =>
                    de.DeviceId   == deviceId &&
                    de.EmployeeId == employeeId);

            if (assignment == null)
                return NotFound("Device not found or not assigned to you.");
            var device = await _context.Device
                .FirstOrDefaultAsync(d => d.Id == deviceId);

            if (device == null)
                return NotFound("Device record not found (it may have been deleted).");
            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                device.Name = dto.Name;
            }

            if (dto.IsEnabled.HasValue)
            {
                device.IsEnabled = dto.IsEnabled.Value;
            }

            if (!string.IsNullOrWhiteSpace(dto.AdditionalProperties))
            {
                device.AdditionalProperties = dto.AdditionalProperties;
            }
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }


