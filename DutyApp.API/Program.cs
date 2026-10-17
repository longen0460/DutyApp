using System.Text;
using DutyApp.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. Подключение к базе данных (PostgreSQL)
// Npgsql (начиная с версии 6.0) сам умеет понимать формат postgres:// от Render
var conn = Environment.GetEnvironmentVariable("DATABASE_URL")
           ?? builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(conn));

// 2. Добавляем контроллеры и Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. Настройка безопасности (JWT)
builder.Services.AddScoped<DutyApp.API.Services.TokenService>();

var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// 4. Автоматически создаем таблицы и пользователей при запуске
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.EnsureCreated();

        // Если пользователей нет — создаем их
        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new DutyApp.API.Models.AppUser { Login = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), Role = "Admin" },
                new DutyApp.API.Models.AppUser { Login = "starosta", PasswordHash = BCrypt.Net.BCrypt.HashPassword("starosta123"), Role = "Starosta" },
                new DutyApp.API.Models.AppUser { Login = "zam", PasswordHash = BCrypt.Net.BCrypt.HashPassword("zam123"), Role = "Zam" }
            );
            db.SaveChanges();
            Console.WriteLine("Пользователи успешно созданы!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при инициализации БД: {ex.Message}");
    }
}

// 5. Настройка HTTP-конвейера (Swagger включен всегда)
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication(); // Проверка токена
app.UseAuthorization();  // Проверка ролей

app.MapControllers();

app.Run();