using DutyApp.API.Data;
using DutyApp.API.Models;
using DutyApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DutyApp.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;

    public AuthController(AppDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    public record LoginDto(string Login, string Password);
    public record CreateUserDto(string Login, string Password, string Role);

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == dto.Login);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized(new { error = "Неверный логин или пароль" });

        return Ok(new { token = _tokens.Create(user), role = user.Role, login = user.Login });
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .Select(u => new { u.Id, u.Login, u.Role })
            .ToListAsync();
        return Ok(users);
    }

    [HttpPost("users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Login) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Логин и пароль обязательны");

        if (await _db.Users.AnyAsync(u => u.Login == dto.Login))
            return BadRequest("Пользователь с таким логином уже существует");

        var user = new AppUser
        {
            Login = dto.Login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role
        };

        _db.Users.Add(user);
        _db.History.Add(new HistoryRecord
        {
            ActorLogin = User.Identity?.Name ?? "?",
            Action = "create_user",
            Details = $"Создан пользователь {dto.Login} с ролью {dto.Role}"
        });
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.Login, user.Role });
    }

    [HttpDelete("users/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        var currentLogin = User.Identity?.Name;
        if (user.Login == currentLogin)
            return BadRequest("Нельзя удалить самого себя");

        _db.Users.Remove(user);
        _db.History.Add(new HistoryRecord
        {
            ActorLogin = currentLogin ?? "?",
            Action = "delete_user",
            Details = $"Удалён пользователь {user.Login}"
        });
        await _db.SaveChangesAsync();
        return NoContent();
    }

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