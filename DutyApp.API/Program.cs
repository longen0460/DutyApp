using System.Text;
using DutyApp.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. Подключение к базе данных (PostgreSQL)
var conn = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(conn) && conn.StartsWith("postgres://"))
{
    // Конвертируем URL от Render в формат Npgsql
    var uri = new Uri(conn);
    var userInfo = uri.UserInfo.Split(':');
    conn = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}
else if (string.IsNullOrEmpty(conn))
{
    conn = builder.Configuration.GetConnectionString("Default");
}

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

// 4. Автоматически создаем таблицы в базе при запуске
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка подключения к БД: {ex.Message}");
    }
}

// 5. Настройка HTTP-конвейера (Swagger включен всегда)
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication(); // Проверка токена
app.UseAuthorization();  // Проверка ролей

app.MapControllers();

app.Run();