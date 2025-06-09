using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api")]
public class WorkController: ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WorkController> _logger;
    public WorkController(ApplicationDbContext context, ILogger<WorkController> logger)
    {
        _context = context;
        _logger = logger;
        
    }
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        _logger.LogInformation("Getting roles");
        var accounts = await _context.Role
            .Select(a => new { a.Id, a.Name})
            .ToListAsync();
        return Ok(accounts);
    }

    [HttpGet("positions")]
    public async Task<IActionResult> GetAllPositions(CancellationToken cancellationToken)
    {
        var positions = await _context.Position
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(cancellationToken);
        return Ok(positions);
    }
}