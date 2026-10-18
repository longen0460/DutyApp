using System.Text;
using DutyApp.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. Подключение к базе данных (Supabase)
var rawConn = postgresql://dutyapp_user:wtV6jo2uABbY5vPOPcJNGmYQTj4W8iuL@dpg-dbaghm49v7es738ef0vg-a/dutyapp_aj4g
// ВАЖНО: замени ТВОЙ_ПАРОЛЬ на пароль от Supabase!

string conn;
if (rawConn.StartsWith("postgresql://") || rawConn.StartsWith("postgres://"))
{
    var uri = new Uri(rawConn);
    var userInfo = uri.UserInfo.Split(':');
    int port = uri.Port > 0 ? uri.Port : 5432;

    // Извлекаем параметры из URL (например, sslmode=require)
    string sslMode = "Require";
    if (uri.Query.Contains("sslmode=require")) sslMode = "Require";

    conn = $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode={sslMode};Trust Server Certificate=true";
}
else
{
    conn = rawConn;
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

// 4. Автоматически создаем таблицы и пользователей при запуске
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.EnsureCreated();

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

// 5. Настройка HTTP-конвейера
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();