using System.Net.Http.Headers;
using System.Net.Http.Json;
using DutyApp.Mobile.Models;

namespace DutyApp.Mobile.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly Session _session;

    // ВАЖНО: сюда мы вставили адрес твоего сервера на Render
    private const string BaseUrl = "https://dutyapp-api.onrender.com/";

    public ApiClient(Session session)
    {
        _session = session;
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    private void ApplyAuth()
    {
        _http.DefaultRequestHeaders.Authorization =
            _session.Token == null ? null : new AuthenticationHeaderValue("Bearer", _session.Token);
    }

    // --- АВТОРИЗАЦИЯ ---
    public async Task<string?> LoginAsync(string login, string password)
    {
        var res = await _http.PostAsJsonAsync("api/auth/login", new { login, password });
        if (!res.IsSuccessStatusCode) return null;

        var data = await res.Content.ReadFromJsonAsync<LoginResponse>();
        if (data == null) return null;

        _session.Token = data.token;
        _session.Role = data.role;
        _session.Login = data.login;
        return data.role;
    }

    // --- СПИСОК ГРУППЫ ---
    public async Task<List<Student>> GetStudentsAsync()
    {
        ApplyAuth();
        return await _http.GetFromJsonAsync<List<Student>>("api/students") ?? new();
    }

    public async Task<Student?> AddStudentAsync(string name)
    {
        ApplyAuth();
        var res = await _http.PostAsJsonAsync("api/students", new { fullName = name, isActive = true });
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<Student>();
    }

    public async Task<bool> DeleteStudentAsync(int id)
    {
        ApplyAuth();
        return (await _http.DeleteAsync($"api/students/{id}")).IsSuccessStatusCode;
    }

    // --- ДЕЖУРСТВА ---
    public async Task<List<DutyEntry>> GetDutiesAsync(int year, int month)
    {
        ApplyAuth();
        return await _http.GetFromJsonAsync<List<DutyEntry>>($"api/duties?year={year}&month={month}") ?? new();
    }

    public async Task<List<DutyEntry>?> AutoAssignAsync(DateTime from, DateTime to, int pairSize, bool skipWeekends)
    {
        ApplyAuth();
        var res = await _http.PostAsJsonAsync("api/duties/auto-assign",
            new { dateFrom = from, dateTo = to, pairSize, skipWeekends });

        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<List<DutyEntry>>();
    }

    // --- БАЛАНС И ИСТОРИЯ ---
    public async Task<List<BalanceItem>> GetBalanceAsync()
    {
        ApplyAuth();
        return await _http.GetFromJsonAsync<List<BalanceItem>>("api/duties/balance") ?? new();
    }

    public async Task<List<HistoryRecord>> GetHistoryAsync()
    {
        ApplyAuth();
        return await _http.GetFromJsonAsync<List<HistoryRecord>>("api/history") ?? new();
    }

    // Вспомогательный класс для ответа от сервера
    private record LoginResponse(string token, string role, string login);
}