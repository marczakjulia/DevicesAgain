using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.DTOs;
using WebApplication1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;

namespace WebApplication1;

[Route("api/accounts")]
[ApiController]
public class AccountsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<Account> _passwordHasher = new();

    public AccountsController(ApplicationDbContext context)
    {
        _context = context;
    }

    //sir from what i understand "Only admins can create, update and delete any accounts"
    //hence this is why i only have one register function which is for the admin
    //ADMIN - CREATE ACCOUNT 
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] CreateAccountDto newUser)
    {
        if (await _context.Account.AnyAsync(a => a.Username == newUser.Username))
            return Conflict(new { message = "Username already exists." });

        var employeeExists = await _context.Employee.AnyAsync(e => e.Id == newUser.EmployeeId);
        if (!employeeExists)
            return BadRequest(new { message = $"No employee found with Id = {newUser.EmployeeId}." });

        var user = new Account
        {
            Username = newUser.Username,
            Password = string.Empty, 
            EmployeeId = newUser.EmployeeId,
            RoleId = 2 //i am automatically making every user a user, not an admit. this can be latered altered by updating
        };
        user.Password = _passwordHasher.HashPassword(user, newUser.Password);
        _context.Account.Add(user);
        await _context.SaveChangesAsync(); 
        return CreatedAtAction(
            nameof(GetAccount),
            new { id = user.Id },
            new { user.Id, user.Username, user.EmployeeId, user.RoleId }
        );
    }

    //ADMIN GET ALL ACCOUNTS
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<object>>> GetAccounts()
    {
        var accounts = await _context.Account
            .Select(a => new { a.Id, a.Username, a.Password })
            .ToListAsync();
        return Ok(accounts);
    }

    //ADMIN GET SPECIFIC ACCOUNT
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAccount(int id)
    {
        var user = await _context.Account
            .Where(a => a.Id == id)
            .Select(a => new { a.Username, a.Password })
            .FirstOrDefaultAsync();
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }
    
    //USER GET HIS INFO
[HttpGet("me")]
[Authorize]
public async Task<IActionResult> GetAccount()
{
    var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(username))
        return Unauthorized();

    var account = await _context.Account
        .Include(a => a.Employee)
            .ThenInclude(e => e.Person)
        .Include(a => a.Employee)
            .ThenInclude(e => e.Position)
        .Include(a => a.Role)
        .FirstOrDefaultAsync(a => a.Username == username);

    if (account == null)
        return NotFound("Account not found.");
    if (account.Employee == null)
        return NotFound("Employee data not found.");
    if (account.Employee.Person == null)
        return NotFound("Employee data not found.");
    if (account.Role == null)
        return NotFound("Role data not found.");

    var employeeDto = new EmployeeDto
    {
        Id = account.Employee.Id,
        FullName = $"{account.Employee.Person.FirstName} {account.Employee.Person.MiddleName} {account.Employee.Person.LastName}",
        Position = new PositionDto
        {
            Id = account.Employee.Position?.Id,
            Name = account.Employee.Position?.Name,
            MinExpYears = account.Employee.Position?.MinExpYears ?? 0
        },
        Person = new PersonDto
        {
            Id = account.Employee.Person.Id,
            FirstName = account.Employee.Person.FirstName,
            MiddleName = account.Employee.Person.MiddleName,
            LastName = account.Employee.Person.LastName,
            Email = account.Employee.Person.Email,
            PhoneNumber = account.Employee.Person.PhoneNumber,
            PassportNumber = account.Employee.Person.PassportNumber
        }
    };

    var accountDto = new AccountDto
    {
        Id = account.Id,
        Username = account.Username,
        RoleName = account.Role.Name,
        Employee = employeeDto
    };

    return Ok(accountDto);
}

[HttpPut("me")]
[Authorize]
public async Task<IActionResult> UpdateMyAccount(UpdateMyAccountDto dto)
    { 
        var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();
        var account = await _context.Account
            .Include(a => a.Employee)
            .ThenInclude(e => e.Person)
            .FirstOrDefaultAsync(a => a.Username == username);
        if (account == null)
            return NotFound("Account not found");
        if (account.Employee == null)
            return NotFound("Employee not found for this account");
        if (account.Employee.Person == null)
            return NotFound("Employee data not found for this account");
        if (!string.IsNullOrWhiteSpace(dto.Username) && dto.Username != username)
        { 
            var conflict = await _context.Account
                .AnyAsync(a => a.Username == dto.Username && a.Id != account.Id);
            if (conflict)
                return BadRequest("Username already taken.");
            account.Username = dto.Username;
        }
        if (!string.IsNullOrWhiteSpace(dto.Password))
        { 
            account.Password = _passwordHasher.HashPassword(account, dto.Password);
        }
        var person = account.Employee.Person;
        if (!string.IsNullOrWhiteSpace(dto.FirstName))
            person.FirstName = dto.FirstName;
        if (!string.IsNullOrWhiteSpace(dto.MiddleName))
            person.MiddleName = dto.MiddleName;
        if (!string.IsNullOrWhiteSpace(dto.LastName))
            person.LastName = dto.LastName;
        if (!string.IsNullOrWhiteSpace(dto.Email))
            person.Email = dto.Email;
        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) 
            person.PhoneNumber = dto.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(dto.PassportNumber))
            person.PassportNumber = dto.PassportNumber;
        await _context.SaveChangesAsync();
        return NoContent();
        }

    //ADMIN UPDATE ACCOUNT
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateAccount(int id, UpdateAccountDto dto)
    {
        var account = await _context.Account.FindAsync(id);
        if (account == null)
            return NotFound();

        if (account.Username != dto.Username)
        {
            if (await _context.Account.AnyAsync(a => a.Username == dto.Username && a.Id != id))
                return BadRequest("Username already exists");

            account.Username = dto.Username;
        }

        if (!string.IsNullOrEmpty(dto.Password))
            account.Password = _passwordHasher.HashPassword(account, dto.Password);

        var role = await _context.Role.FindAsync(dto.RoleId);
        if (role == null)
            return BadRequest("Role does not exist");

        account.RoleId = dto.RoleId;

        _context.Account.Update(account);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    //ADMIN DELETE ACCOUNT 
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Account.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        _context.Account.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    
}

