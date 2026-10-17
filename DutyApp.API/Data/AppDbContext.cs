using DutyApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DutyApp.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<DutyEntry> Duties => Set<DutyEntry>();
    public DbSet<HistoryRecord> History => Set<HistoryRecord>();
}