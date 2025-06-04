namespace WebApplication1.Tokens;

public interface ITokenService
{
    string GenerateToken(string username, string role);
}