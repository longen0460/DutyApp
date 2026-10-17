namespace DutyApp.API.Models;

public class DutyEntry
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public int StudentId1 { get; set; }
    public int? StudentId2 { get; set; } // пара необязательна (если дежурят по 2)
    public bool IsAutoAssigned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}