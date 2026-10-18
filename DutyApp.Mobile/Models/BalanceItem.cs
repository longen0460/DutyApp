namespace DutyApp.Mobile.Models;

public class BalanceItem
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public bool IsAvailable { get; set; } = true;
    public int Count { get; set; }
}