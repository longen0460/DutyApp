namespace DutyApp.Mobile.Models;

public class HistoryRecord
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string ActorLogin { get; set; } = "";
    public string Action { get; set; } = "";
    public string Details { get; set; } = "";
}