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
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(ApplicationDbContext context, ILogger<AccountsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    //sir from what i understand "Only admins can create, update and delete any accounts"
    //hence this is why i only have one register function which is for the admin
    //ADMIN - CREATE ACCOUNT 
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] CreateAccountDto newUser)
    {
        _logger.LogInformation("Registering new account");
        if (await _context.Account.AnyAsync(a => a.Username == newUser.Username))
        {
            _logger.LogInformation("User with username already exists");
            return Conflict(new { message = "Username already exists" });
        }

        var employeeExists = await _context.Employee.AnyAsync(e => e.Id == newUser.EmployeeId);
        if (!employeeExists)
        {
            _logger.LogInformation("Employee with id does not exist");
            return BadRequest(new { message = $"No employee found with Id = {newUser.EmployeeId}." });
        }

        var user = new Account
        {
            Username = newUser.Username,
            Password = string.Empty, 
            EmployeeId = newUser.EmployeeId,
            RoleId = newUser.RoleId,
        };
        user.Password = _passwordHasher.HashPassword(user, newUser.Password);
        _context.Account.Add(user);
        await _context.SaveChangesAsync(); 
        _logger.LogInformation("Registered new account");
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
            .Include(a => a.Role)
            .Select(a => new { a.Id, a.Username })
            .ToListAsync();
        return Ok(accounts);
    }

    //ADMIN GET SPECIFIC ACCOUNT
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAccount(int id)
    {
        var user = await _context.Account
            .Include(a => a.Role)
            .Where(a => a.Id == id)
            .Select(a => new { a.Username, Role = a.Role.Name })
            .FirstOrDefaultAsync();
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    
// UPDATE ACCOUNT
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateAccount(int id, UpdateAccountDto dto)
    {
        _logger.LogInformation("Starting {ActionName}", nameof(UpdateAccount));
        _logger.LogInformation("Updating account with id {id}", id);
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin)
        {
            _logger.LogInformation("User with id {id} does not have an admin role", id);
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(username))
                return Forbid();
            var account = await _context.Account.FirstOrDefaultAsync(a => a.Username == username);
            if (account == null || account.Id != id)
                return Unauthorized();
        }
        var acc = await _context.Account.FindAsync(id);
        if (acc == null)
        {
            _logger.LogWarning("Account {id} not found", id);
            return NotFound();
        }

        if (acc.Username != dto.Username)
        {
            if (await _context.Account.AnyAsync(a => a.Username == dto.Username && a.Id != id))
                return BadRequest("Username already exists");
            acc.Username = dto.Username;
        }

        if (!string.IsNullOrEmpty(dto.Password))
            acc.Password = _passwordHasher.HashPassword(acc, dto.Password);

        if (isAdmin)
        {
            var role = await _context.Role.FindAsync(dto.RoleId);
            if (role == null)
                return BadRequest("Role does not exist");
            acc.RoleId = dto.RoleId;
        }
        _logger.LogInformation("Updating account with id {id}", id);
        _context.Account.Update(acc);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in UpdateAccount");
            throw;
        }

        return NoContent();
    }

    //ADMIN DELETE ACCOUNT 
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        _logger.LogInformation("Deleting account with id {id}", id);
        var user = await _context.Account.FindAsync(id);
        if (user == null)
        {
            _logger.LogInformation("User with id {id} does not exist", id);
            return NotFound();
        }

        _context.Account.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    
    
}

