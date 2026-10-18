using System.Net.Http.Headers;
using System.Net.Http.Json;
using DutyApp.Mobile.Models;

namespace DutyApp.Mobile.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly Session _session;

    private const string BaseUrl = "https://dutyapp-api-go49.onrender.com/";

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

    public async Task<string?> LoginAsync(string login, string password)
    {
        try
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
        catch { return null; }
    }

    public async Task<List<AppUserDto>> GetUsersAsync()
    {
        ApplyAuth();
        return await _http.GetFromJsonAsync<List<AppUserDto>>("api/auth/users") ?? new();
    }

    public async Task<bool> CreateUserAsync(string login, string password, string role)
    {
        ApplyAuth();
        var res = await _http.PostAsJsonAsync("api/auth/users", new { login, password, role });
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        ApplyAuth();
        return (await _http.DeleteAsync($"api/auth/users/{id}")).IsSuccessStatusCode;
    }

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

    public async Task<List<DutyEntry>> GetDutiesAsync(int year, int month)
    {
        ApplyAuth();
        return await _http.GetFromJsonAsync<List<DutyEntry>>($"api/duties?year={year}&month={month}") ?? new();
    }

    public async Task<List<DutyEntry>?> AutoAssignAsync(DateTime from, DateTime to, int pairSize, bool skipWeekends)
    {
        try
        {
            ApplyAuth();
            var requestData = new
            {
                dateFrom = from.ToString("yyyy-MM-dd"),
                dateTo = to.ToString("yyyy-MM-dd"),
                pairSize = pairSize,
                skipWeekends = skipWeekends
            };

            var res = await _http.PostAsJsonAsync("api/duties/auto-assign", requestData);
            if (!res.IsSuccessStatusCode)
            {
                var errorText = await res.Content.ReadAsStringAsync();
                Console.WriteLine($"Ошибка автораспределения: {res.StatusCode} - {errorText}");
                return null;
            }
            return await res.Content.ReadFromJsonAsync<List<DutyEntry>>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Исключение при автораспределении: {ex.Message}");
            return null;
        }
    }

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

    public async Task<List<DutyEntry>> GetStudentHistoryAsync(int studentId)
    {
        ApplyAuth();
        return await _http.GetFromJsonAsync<List<DutyEntry>>($"api/duties/history/{studentId}") ?? new();
    }

    private record LoginResponse(string token, string role, string login);

    public class AppUserDto
    {
        public int Id { get; set; }
        public string Login { get; set; } = "";
        public string Role { get; set; } = "";
    }
}