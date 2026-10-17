using DutyApp.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DutyApp.API.Controllers;

[ApiController]
[Route("api/history")]
[Authorize(Roles = "Admin,Starosta,Zam")]
public class HistoryController : ControllerBase
{
    private readonly AppDbContext _db;
    public HistoryController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await _db.History.OrderByDescending(h => h.Timestamp).Take(500).ToListAsync());
}