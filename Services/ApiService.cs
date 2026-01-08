using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

public class ApiService
{
    private readonly HttpClient _httpClient; 

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(AppConfig.BaseUrl) // 객체 초기화 문법
        };
    }

    // 폴링 : 서버에 해야할 일이 있는지 주기적으로 물어 봄
    public async Task<WorkOrderDto> PollWorkOrderAsync()
    {
        try
        {
            // http://localhost:8111/api/mes/machine/poll  RequestParam 방식
            var url = $"machine/poll?machineId={Uri.EscapeDataString(AppConfig.MachineId)}";

        var response = await _httpClient.GetAsync(url);
        if (response.StatusCode == System.Net.HttpStatusCode.OK)  // 응답이 200
        {
            return await response.Content.ReadFromJsonAsync<WorkOrderDto>();
        }
            
        } catch (Exception ex)
        {
            Console.WriteLine($"[Error] API 통신 실패 : {ex.Message}");
        }
        return null;
    }

    // 생산 실적 보고 (POST)
    public async Task<bool> ReportProductionAsync(ProductionReportDto report)
    {
      try
      {
        var response = await _httpClient.PostAsJsonAsync("machine/report", report);
        return response.IsSuccessStatusCode;
      } catch (Exception ex)
      {
        Console.WriteLine($"[Error] 실적 보고 실패: {ex.Message}");
        return false;
      }
    }
}