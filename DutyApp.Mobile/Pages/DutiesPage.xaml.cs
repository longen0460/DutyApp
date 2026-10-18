using DutyApp.Mobile.Services;

namespace DutyApp.Mobile.Pages;

public partial class DutiesPage : ContentPage
{
    private readonly ApiClient _api;
    private readonly Session _session;
    private DateTime _currentDate = DateTime.Now;

    public DutiesPage(ApiClient api, Session session)
    {
        InitializeComponent();
        _api = api;
        _session = session;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateMonthLabel();
        await LoadData();
    }

    private void UpdateMonthLabel()
    {
        string[] months = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
                            "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
        MonthLabel.Text = $"{months[_currentDate.Month - 1]} {_currentDate.Year}";
    }

    private async Task LoadData()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        try
        {
            var duties = await _api.GetDutiesAsync(_currentDate.Year, _currentDate.Month);
            var students = await _api.GetStudentsAsync();

            var displayList = duties.Select(d => new
            {
                d.Date,
                DisplayNames = students.Where(s => s.Id == d.StudentId1 || s.Id == d.StudentId2)
                                       .Select(s => s.FullName)
                                       .Aggregate("", (a, b) => string.IsNullOrEmpty(a) ? b : $"{a}, {b}")
            }).ToList();

            ListView.ItemsSource = displayList;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить данные: {ex.Message}", "ОК");
        }

        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
    }

    private async void OnPrevMonth(object sender, EventArgs e)
    {
        _currentDate = _currentDate.AddMonths(-1);
        UpdateMonthLabel();
        await LoadData();
    }

    private async void OnNextMonth(object sender, EventArgs e)
    {
        _currentDate = _currentDate.AddMonths(1);
        UpdateMonthLabel();
        await LoadData();
    }

    private async void OnAutoAssignClicked(object sender, EventArgs e)
    {
        try
        {
            // Спрашиваем только размер пары
            string pairStr = await DisplayPromptAsync("Автораспределение", "Сколько человек в паре? (1 или 2)", initialValue: "2");
            if (string.IsNullOrEmpty(pairStr)) return;

            if (!int.TryParse(pairStr, out int pairSize)) pairSize = 2;

            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            var today = DateTime.Today;
            var result = await _api.AutoAssignAsync(today, today, pairSize, skipWeekends: false);

            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            if (result != null)
            {
                await DisplayAlert("Успех", $"Дежурные на сегодня назначены! Добавлено: {result.Count}", "ОК");
                await LoadData();
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось распределить. Проверьте, есть ли студенты в списке.", "ОК");
            }
        }
        catch (Exception ex)
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            await DisplayAlert("Ошибка", $"Произошла ошибка: {ex.Message}", "ОК");
        }
    }
}