using DutyApp.API.Data;
using DutyApp.API.Models;
using DutyApp.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DutyApp.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;
    public AuthController(AppDbContext db, TokenService tokens) { _db = db; _tokens = tokens; }

    public record LoginDto(string Login, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == dto.Login);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { error = "Неверный логин или пароль" });

        return Ok(new { token = _tokens.Create(user), role = user.Role, login = user.Login });
    }

    // Вызвать ОДИН РАЗ через Swagger, чтобы создать трех пользователей
    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        if (await _db.Users.AnyAsync()) return BadRequest("Уже инициализировано");
        _db.Users.AddRange(
            new AppUser { Login = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), Role = "Admin" },
            new AppUser { Login = "starosta", PasswordHash = BCrypt.Net.BCrypt.HashPassword("starosta123"), Role = "Starosta" },
            new AppUser { Login = "zam", PasswordHash = BCrypt.Net.BCrypt.HashPassword("zam123"), Role = "Zam" }
        );
        await _db.SaveChangesAsync();
        return Ok("Созданы admin/starosta/zam");
    }
}