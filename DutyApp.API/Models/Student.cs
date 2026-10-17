namespace DutyApp.API.Models;

public class Student
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public bool IsActive { get; set; } = true; // можно временно исключить из дежурств
}