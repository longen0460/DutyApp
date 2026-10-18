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

        var duties = await _api.GetDutiesAsync(_currentDate.Year, _currentDate.Month);
        var students = await _api.GetStudentsAsync();

        // Склеиваем данные для отображения имен вместо ID
        var displayList = duties.Select(d => new
        {
            d.Date,
            DisplayNames = students.Where(s => s.Id == d.StudentId1 || s.Id == d.StudentId2)
                                   .Select(s => s.FullName)
                                   .Aggregate((a, b) => $"{a}, {b}")
        }).ToList();

        ListView.ItemsSource = displayList;

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
        string fromStr = await DisplayPromptAsync("Автораспределение", "С какой даты? (дд.мм.гггг)", initialValue: DateTime.Now.ToString("dd.MM.yyyy"));
        if (string.IsNullOrEmpty(fromStr)) return;

        string toStr = await DisplayPromptAsync("Автораспределение", "По какую дату? (дд.мм.гггг)", initialValue: DateTime.Now.AddDays(7).ToString("dd.MM.yyyy"));
        if (string.IsNullOrEmpty(toStr)) return;

        string pairStr = await DisplayPromptAsync("Автораспределение", "Сколько человек в паре? (1 или 2)", initialValue: "2");
        if (!int.TryParse(pairStr, out int pairSize)) pairSize = 2;

        if (DateTime.TryParseExact(fromStr, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime fromDate) &&
            DateTime.TryParseExact(toStr, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime toDate))
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;

            var result = await _api.AutoAssignAsync(fromDate, toDate, pairSize, true);

            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            if (result != null)
            {
                await DisplayAlert("Успех", $"Создано дежурств: {result.Count}", "ОК");
                await LoadData();
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось распределить. Возможно, база пуста.", "ОК");
            }
        }
        else
        {
            await DisplayAlert("Ошибка", "Неверный формат даты", "ОК");
        }
    }
}