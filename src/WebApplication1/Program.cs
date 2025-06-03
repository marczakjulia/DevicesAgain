using System.Text;
using Microsoft.EntityFrameworkCore;
using WebApplication1;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Models;
using WebApplication1.Tokens;

var builder = WebApplication.CreateBuilder(args);
var jwtConfigData = builder.Configuration.GetSection("Jwt");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new Exception("No connection string");
builder.Services.AddDbContext<ApplicationDbContext>(O => O.UseSqlServer(connectionString));
builder.Services.Configure<JwtOptions>(jwtConfigData);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfigData["Issuer"],
        ValidAudience = jwtConfigData["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfigData["Key"])),
        ClockSkew = TimeSpan.FromMinutes(10)
    };
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddAuthentication();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();