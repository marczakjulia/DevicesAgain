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
            return Conflict(new { message = "Username already exists" });

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

    // GET SPECIFIC ACCOUNT
    [HttpGet("{id}")]
    [Authorize]  
    public async Task<IActionResult> GetAccount(int id)
    {
        bool isAdmin = User.IsInRole("Admin");
        if (!isAdmin)
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(username))
                return Forbid();
            var selfAccount = await _context.Account
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Username == username);

            if (selfAccount == null || selfAccount.Id != id)
            {
                return Unauthorized();
            }
        }
        var user = await _context.Account
            .Where(a => a.Id == id)
            .Select(a => new 
            { 
                a.Username, 
                a.Password 
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(); 

        return Ok(user);
    }

    
// UPDATE ACCOUNT
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateAccount(int id, UpdateAccountDto dto)
    {
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin)
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(username))
                return Forbid();
            var account = await _context.Account.FirstOrDefaultAsync(a => a.Username == username);
            if (account == null || account.Id != id)
                return Unauthorized();
        }
        var acc = await _context.Account.FindAsync(id);
        if (acc == null)
            return NotFound();

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

        _context.Account.Update(acc);
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

