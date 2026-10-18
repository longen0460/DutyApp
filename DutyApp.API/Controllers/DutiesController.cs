using DutyApp.API.Data;
using DutyApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DutyApp.API.Controllers;

[ApiController]
[Route("api/duties")]
[Authorize(Roles = "Admin,Starosta,Zam")]
public class DutiesController : ControllerBase
{
    private readonly AppDbContext _db;
    public DutiesController(AppDbContext db) => _db = db;

    private void Log(string action, string details) =>
        _db.History.Add(new HistoryRecord
        {
            ActorLogin = User.Identity?.Name ?? "?",
            Action = action,
            Details = details
        });

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? year, [FromQuery] int? month)
    {
        var q = _db.Duties.AsQueryable();
        if (year.HasValue && month.HasValue)
            q = q.Where(d => d.Date.Year == year && d.Date.Month == month);
        return Ok(await q.OrderBy(d => d.Date).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Assign([FromBody] DutyEntry entry)
    {
        entry.Id = 0;
        entry.IsAutoAssigned = false;
        entry.Date = DateTime.SpecifyKind(entry.Date, DateTimeKind.Utc);
        entry.CreatedAt = DateTime.UtcNow;
        _db.Duties.Add(entry);
        Log("assign_duty", $"{entry.Date:yyyy-MM-dd} -> {entry.StudentId1}, {entry.StudentId2}");
        await _db.SaveChangesAsync();
        return Ok(entry);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var d = await _db.Duties.FindAsync(id);
        if (d == null) return NotFound();
        _db.Duties.Remove(d);
        Log("delete_duty", $"Удалено дежурство id={id}");
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Автоматическое распределение с честным рандомом и балансом нагрузки
    [HttpPost("auto-assign")]
    public async Task<IActionResult> AutoAssign([FromBody] AutoAssignRequest req)
    {
        var students = await _db.Students.Where(s => s.IsActive).ToListAsync();
        if (students.Count == 0) return BadRequest("Нет студентов в списке");

        var duties = await _db.Duties.ToListAsync();
        var load = students.ToDictionary(
            s => s.Id,
            s => duties.Count(d => d.StudentId1 == s.Id || d.StudentId2 == s.Id));

        var rnd = new Random();
        var created = new List<DutyEntry>();

        for (var date = req.DateFrom.Date; date <= req.DateTo.Date; date = date.AddDays(1))
        {
            if (req.SkipWeekends && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
                continue;

            int minLoad = load.Values.Min();
            var candidates = students.Where(s => load[s.Id] == minLoad).ToList();

            var pair = new List<int>();
            for (int i = 0; i < req.PairSize && candidates.Count > 0; i++)
            {
                var idx = rnd.Next(candidates.Count);
                var pick = candidates[idx];
                candidates.RemoveAt(idx);
                pair.Add(pick.Id);
                load[pick.Id]++;
            }

            var entry = new DutyEntry
            {
                Date = DateTime.SpecifyKind(date, DateTimeKind.Utc),
                StudentId1 = pair[0],
                StudentId2 = pair.Count > 1 ? pair[1] : null,
                IsAutoAssigned = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.Duties.Add(entry);
            created.Add(entry);
        }

        Log("auto_assign", $"Автораспределение {req.DateFrom:yyyy-MM-dd}..{req.DateTo:yyyy-MM-dd}, пар={req.PairSize}, создано {created.Count}");
        await _db.SaveChangesAsync();
        return Ok(created);
    }

    [HttpGet("balance")]
    public async Task<IActionResult> Balance()
    {
        var students = await _db.Students.ToListAsync();
        var duties = await _db.Duties.ToListAsync();
        var result = students.Select(s => new
        {
            s.Id,
            s.FullName,
            Count = duties.Count(d => d.StudentId1 == s.Id || d.StudentId2 == s.Id)
        }).OrderByDescending(x => x.Count).ToList();
        return Ok(result);
    }

    [HttpGet("history/{studentId}")]
    public async Task<IActionResult> GetStudentHistory(int studentId)
    {
        var duties = await _db.Duties
            .Where(d => d.StudentId1 == studentId || d.StudentId2 == studentId)
            .OrderByDescending(d => d.Date)
            .ToListAsync();
        return Ok(duties);
    }
}

public record AutoAssignRequest(DateTime DateFrom, DateTime DateTo, int PairSize, bool SkipWeekends);