using DutyApp.Mobile.Models;
using DutyApp.Mobile.Services;

namespace DutyApp.Mobile.Pages;

public partial class BalancePage : ContentPage
{
    private readonly ApiClient _api;
    private readonly Session _session;

    public BalancePage(ApiClient api, Session session)
    {
        InitializeComponent();
        _api = api;
        _session = session;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadData();
    }

    private async Task LoadData()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        var balance = await _api.GetBalanceAsync();
        ListView.ItemsSource = balance;

        var todayDuties = await _api.GetTodayDutiesAsync();
        var students = await _api.GetStudentsAsync();

        if (todayDuties.Count == 0)
        {
            TodayLabel.Text = "Не назначены";
        }
        else
        {
            var names = todayDuties
                .SelectMany(d => new[] { d.StudentId1, d.StudentId2 })
                .Where(id => id.HasValue)
                .Select(id => students.FirstOrDefault(s => s.Id == id)?.FullName ?? "?")
                .ToList();
            TodayLabel.Text = string.Join(", ", names);
        }

        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
    }

    private async void OnAssignClicked(object sender, EventArgs e)
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        var ok = await _api.AssignTodayAsync();
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        if (ok)
            await LoadData();
        else
            await DisplayAlert("Ошибка", "Не удалось назначить (нужно минимум 2 доступных студента).", "ОК");
    }

    private async void OnToggleAvailability(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        var id = (int)btn.CommandParameter;
        await _api.ToggleAvailabilityAsync(id);
        await LoadData();
    }

    private async void OnHistoryClicked(object sender, EventArgs e)
    {
        var btn = (Button)sender;
        var id = (int)btn.CommandParameter;
        var students = await _api.GetStudentsAsync();
        var student = students.FirstOrDefault(s => s.Id == id);
        if (student == null) return;

        var history = await _api.GetStudentHistoryAsync(id);

        if (history.Count == 0)
        {
            bool add = await DisplayAlert(student.FullName, "Ещё не дежурил. Добавить дежурство?", "Да", "Нет");
            if (add)
            {
                string dateStr = await DisplayPromptAsync("Дата дежурства", "Введите дату (дд.мм.гггг):", initialValue: DateTime.Today.ToString("dd.MM.yyyy"));
                if (DateTime.TryParseExact(dateStr, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime dt))
                {
                    var ok = await _api.AddManualDutyAsync(id, dt);
                    if (!ok) await DisplayAlert("Ошибка", "Не удалось добавить (возможно, занято).", "ОК");
                }
            }
            return;
        }

        string[] options = new string[history.Count + 1];
        options[0] = "➕ Добавить дежурство";
        for (int i = 0; i < history.Count; i++)
            options[i + 1] = "❌ Удалить: " + history[i].Date.ToString("dd.MM.yyyy");

        string action = await DisplayActionSheet(student.FullName, "Отмена", null, options);

        if (string.IsNullOrEmpty(action) || action == "Отмена") return;

        if (action.StartsWith("➕ Добавить"))
        {
            string dateStr = await DisplayPromptAsync("Дата", "Введите дату (дд.мм.гггг):", initialValue: DateTime.Today.ToString("dd.MM.yyyy"));
            if (DateTime.TryParseExact(dateStr, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime dt))
            {
                var ok = await _api.AddManualDutyAsync(id, dt);
                if (!ok) await DisplayAlert("Ошибка", "Не удалось добавить.", "ОК");
            }
        }
        else if (action.StartsWith("❌"))
        {
            int idx = Array.IndexOf(options, action) - 1;
            var duty = history[idx];
            var ok = await _api.DeleteDutyFromHistoryAsync(id, duty.Id);
            if (!ok) await DisplayAlert("Ошибка", "Не удалось удалить.", "ОК");
        }

        await LoadData();
    }
}