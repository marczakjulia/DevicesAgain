using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.DTOs;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/employees")]
    public class EmployeesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public EmployeesController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllEmployees(CancellationToken cancellationToken)
        {
            try
            {
                var employees = await _context.Employee
                    .Include(e => e.Person)
                    .Select(e => new
                    {
                        Id = e.Id,
                        Name = e.Person.FirstName + " " + (e.Person.MiddleName ?? "") + " " + e.Person.LastName
                    })
                    .ToListAsync(cancellationToken);
                return Ok(employees);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
        
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetEmployeeById(int id, CancellationToken cancellationToken)
        {
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin)
            {
                var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Forbid();
                var account = await _context.Account
                    .FirstOrDefaultAsync(a => a.Username == username, cancellationToken);
                if (account == null || account.EmployeeId != id)
                    return Forbid();
            }
            var emp = await _context.Employee
                .Where(e => e.Id == id)
                .Include(e => e.Person)
                .Include(e => e.Position)
                .Select(e => new
                {
                    Person = new
                    {
                        PassportNumber = e.Person.PassportNumber,
                        FirstName = e.Person.FirstName,
                        MiddleName = e.Person.MiddleName,
                        LastName = e.Person.LastName,
                        PhoneNumber = e.Person.PhoneNumber,
                        Email = e.Person.Email
                    },
                    Salary = e.Salary,
                    Position = new
                    {
                        Id = e.Position.Id,
                        Name = e.Position.Name
                    },
                    HireDate = e.HireDate
                })
                .FirstOrDefaultAsync(cancellationToken);
            if (emp is null)
                return NotFound($"Employee {id} not found");
            return Ok(emp);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto dto, CancellationToken cancellationToken)
        {
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin)
            {
                var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(username))
                    return Forbid();
                var account = await _context.Account.FirstOrDefaultAsync(a => a.Username == username, cancellationToken);
                if (account == null || account.EmployeeId != id)
                    return Forbid();
            }
            var employee = await _context.Employee.Include(e => e.Person).FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            if (employee == null)
                return NotFound($"Employee {id} not found");
            if (!string.IsNullOrWhiteSpace(dto.FirstName)) employee.Person.FirstName = dto.FirstName;
            if (!string.IsNullOrWhiteSpace(dto.MiddleName)) employee.Person.MiddleName = dto.MiddleName;
            if (!string.IsNullOrWhiteSpace(dto.LastName)) employee.Person.LastName = dto.LastName;
            if (!string.IsNullOrWhiteSpace(dto.Email)) employee.Person.Email = dto.Email;
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) employee.Person.PhoneNumber = dto.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(dto.PassportNumber)) employee.Person.PassportNumber = dto.PassportNumber;
            if (dto.PositionId.HasValue) employee.PositionId = dto.PositionId.Value;
            if (isAdmin && dto.Salary.HasValue) employee.Salary = dto.Salary.Value; //only admin can update salary
            await _context.SaveChangesAsync(cancellationToken);
            return NoContent();
        }
    }
} 