using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.DTOs;
using WebApplication1.Models;
using WebApplication1.Tokens;

namespace WebApplication1;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly PasswordHasher<Account> _passwordHasher = new() ;

    public AuthController(ApplicationDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }
    [HttpPost]
    public async Task<IActionResult> Auth(LoginDto user, CancellationToken cancellationToken)
    {
        var foundUser = await _context.Account.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username.Equals(user.Username), cancellationToken);
        if(foundUser == null)
        {
            return Unauthorized();
        }
        var verificationResult = _passwordHasher.VerifyHashedPassword(foundUser, foundUser.Password, user.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }

        var accessToken = new TokenReposndeDto
        {
            AccessToken = _tokenService.GenerateToken(
                foundUser.Username,
                foundUser.Role.Name
            ),
        };
        return Ok(accessToken);
    }
}