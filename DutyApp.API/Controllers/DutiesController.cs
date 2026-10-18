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

    [HttpGet("today")]
    public async Task<IActionResult> GetToday()
    {
        var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);
        return Ok(await _db.Duties.Where(d => d.Date == today).ToListAsync());
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

    [HttpPost("assign-today")]
    public async Task<IActionResult> AssignToday()
    {
        var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);

        var existing = await _db.Duties.Where(d => d.Date == today).ToListAsync();
        _db.Duties.RemoveRange(existing);

        var available = await _db.Students
            .Where(s => s.IsActive && s.IsAvailable)
            .ToListAsync();

        if (available.Count < 2) return BadRequest("Недостаточно доступных студентов");

        var allDuties = await _db.Duties.ToListAsync();
        var load = available.ToDictionary(
            s => s.Id,
            s => allDuties.Count(d => d.StudentId1 == s.Id || d.StudentId2 == s.Id));

        var rnd = new Random();
        var chosen = new List<int>();
        for (int i = 0; i < 2; i++)
        {
            var remaining = load.Where(kv => !chosen.Contains(kv.Key)).ToList();
            if (remaining.Count == 0) break;
            int minLoad = remaining.Min(kv => kv.Value);
            var candidates = remaining.Where(kv => kv.Value == minLoad).Select(kv => kv.Key).ToList();
            var pick = candidates[rnd.Next(candidates.Count)];
            chosen.Add(pick);
        }

        if (chosen.Count < 2) return BadRequest("Не удалось выбрать двух");

        var entry = new DutyEntry
        {
            Date = today,
            StudentId1 = chosen[0],
            StudentId2 = chosen[1],
            IsAutoAssigned = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Duties.Add(entry);
        Log("assign_today", $"Дежурные на сегодня: {chosen[0]}, {chosen[1]}");
        await _db.SaveChangesAsync();

        return Ok(entry);
    }

    [HttpPost("auto-assign")]
    public async Task<IActionResult> AutoAssign([FromBody] AutoAssignRequest req)
    {
        var students = await _db.Students.Where(s => s.IsActive && s.IsAvailable).ToListAsync();
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
            s.IsAvailable,
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

    [HttpDelete("history/{studentId}/{dutyId}")]
    public async Task<IActionResult> DeleteFromHistory(int studentId, int dutyId)
    {
        var duty = await _db.Duties.FindAsync(dutyId);
        if (duty == null) return NotFound();

        if (duty.StudentId1 == studentId)
        {
            if (duty.StudentId2 != null)
            {
                duty.StudentId1 = duty.StudentId2.Value;
                duty.StudentId2 = null;
            }
            else
            {
                _db.Duties.Remove(duty);
            }
        }
        else if (duty.StudentId2 == studentId)
        {
            duty.StudentId2 = null;
        }

        Log("delete_from_history", $"Удалено дежурство id={dutyId} у студента id={studentId}");
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("manual")]
    public async Task<IActionResult> AddManual([FromBody] ManualDutyRequest req)
    {
        var student = await _db.Students.FindAsync(req.StudentId);
        if (student == null) return NotFound("Студент не найден");

        var date = DateTime.SpecifyKind(req.Date.Date, DateTimeKind.Utc);

        var existing = await _db.Duties.FirstOrDefaultAsync(d => d.Date == date && (d.StudentId1 == req.StudentId || d.StudentId2 == req.StudentId));
        if (existing != null) return BadRequest("Студент уже дежурит в этот день");

        var dutyOnDate = await _db.Duties.FirstOrDefaultAsync(d => d.Date == date);
        if (dutyOnDate != null)
        {
            if (dutyOnDate.StudentId2 == null)
            {
                dutyOnDate.StudentId2 = req.StudentId;
                await _db.SaveChangesAsync();
                return Ok(dutyOnDate);
            }
            else
            {
                return BadRequest("На этот день уже назначены 2 человека");
            }
        }

        var entry = new DutyEntry
        {
            Date = date,
            StudentId1 = req.StudentId,
            IsAutoAssigned = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.Duties.Add(entry);
        Log("manual_duty", $"Вручную добавлено дежурство: {student.FullName} на {date:yyyy-MM-dd}");
        await _db.SaveChangesAsync();
        return Ok(entry);
    }
}

public record AutoAssignRequest(DateTime DateFrom, DateTime DateTo, int PairSize, bool SkipWeekends);
public record ManualDutyRequest(int StudentId, DateTime Date);