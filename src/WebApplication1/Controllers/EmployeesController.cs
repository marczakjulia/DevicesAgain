using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.DTOs;
using Microsoft.Extensions.Logging;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/employees")]
    public class EmployeesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployeesController> _logger;
        public EmployeesController(ApplicationDbContext context, ILogger<EmployeesController> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllEmployees(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting all employees");
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
                _logger.LogError(ex, "Error getting all employees");
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
                var username = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(username))
                    return Forbid();
                var account = await _context.Account.FirstOrDefaultAsync(a => a.Username == username, cancellationToken);
                if (account == null || account.EmployeeId != id)
                    return Forbid();
            }
            var emp = await _context.Employee
                .Where(e => e.Id == id)
                .Include(e => e.Person)
                .Include(e => e.Position)
                .Select(e => new
                {
                    person = new
                    {
                        e.Person.PassportNumber,
                        e.Person.FirstName,
                        e.Person.MiddleName,
                        e.Person.LastName,
                        e.Person.PhoneNumber,
                        e.Person.Email
                    },
                    salary = e.Salary,
                    position = e.Position.Name,
                    hireDate = e.HireDate
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
            _logger.LogInformation("Starting Update Employee");
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
            {
                _logger.LogWarning("Employee {id} not found", id);
                return NotFound($"Employee {id} not found");
            }
            if (!string.IsNullOrWhiteSpace(dto.FirstName)) employee.Person.FirstName = dto.FirstName;
            if (!string.IsNullOrWhiteSpace(dto.MiddleName)) employee.Person.MiddleName = dto.MiddleName;
            if (!string.IsNullOrWhiteSpace(dto.LastName)) employee.Person.LastName = dto.LastName;
            if (!string.IsNullOrWhiteSpace(dto.Email)) employee.Person.Email = dto.Email;
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) employee.Person.PhoneNumber = dto.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(dto.PassportNumber)) employee.Person.PassportNumber = dto.PassportNumber;
            if (dto.PositionId.HasValue) employee.PositionId = dto.PositionId.Value;
            if (isAdmin && dto.Salary.HasValue) employee.Salary = dto.Salary.Value; //only admin can update salary
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in UpdateEmployee");
                return Problem(ex.Message);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting {ActionName}", nameof(CreateEmployee));
            if (dto.Person == null)
                return BadRequest("Person data is required");
            var position = await _context.Position.FindAsync(new object[] { dto.PositionId }, cancellationToken);
            if (position == null)
                return BadRequest($"Position {dto.PositionId} not found");
            var person = new Person
            {
                PassportNumber = dto.Person.PassportNumber,
                FirstName = dto.Person.FirstName,
                MiddleName = dto.Person.MiddleName,
                LastName = dto.Person.LastName,
                PhoneNumber = dto.Person.PhoneNumber,
                Email = dto.Person.Email
            };
            _context.Person.Add(person);
            await _context.SaveChangesAsync(cancellationToken);
            var employee = new Employee
            {
                PersonId = person.Id,
                Salary = dto.Salary,
                PositionId = dto.PositionId,
                HireDate = DateTime.UtcNow
            };
            _context.Employee.Add(employee);
            await _context.SaveChangesAsync(cancellationToken);
            var result = new
            {
                id = employee.Id,
                person = new
                {
                    person.PassportNumber,
                    person.FirstName,
                    person.MiddleName,
                    person.LastName,
                    person.PhoneNumber,
                    person.Email
                },
                salary = employee.Salary,
                positionId = employee.PositionId,
                hireDate = employee.HireDate
            };
            _logger.LogInformation("Created employee {id}", employee.Id);
            return Created($"/api/employees/{employee.Id}", result);
        }
    }
} 