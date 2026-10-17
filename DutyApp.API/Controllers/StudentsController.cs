using DutyApp.API.Data;
using DutyApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DutyApp.API.Controllers;

[ApiController]
[Route("api/students")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public StudentsController(AppDbContext db) => _db = db;

    private void Log(string action, string details) =>
        _db.History.Add(new HistoryRecord
        {
            ActorLogin = User.Identity?.Name ?? "?",
            Action = action,
            Details = details
        });

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Students.OrderBy(s => s.FullName).ToListAsync());

    [HttpPost]
    [Authorize(Roles = "Admin")] // Только админ
    public async Task<IActionResult> Add([FromBody] Student s)
    {
        s.Id = 0;
        _db.Students.Add(s);
        Log("add_student", $"Добавлен: {s.FullName}");
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] Student s)
    {
        var db = await _db.Students.FindAsync(id);
        if (db == null) return NotFound();
        db.FullName = s.FullName;
        db.IsActive = s.IsActive;
        Log("update_student", $"Изменён id={id}: {s.FullName}");
        await _db.SaveChangesAsync();
        return Ok(db);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _db.Students.FindAsync(id);
        if (s == null) return NotFound();
        _db.Students.Remove(s);
        Log("delete_student", $"Удалён: {s.FullName}");
        await _db.SaveChangesAsync();
        return NoContent();
    }
}