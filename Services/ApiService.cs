using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;

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
    // 로그인 기능
    public async Task<bool> LoginAsync(string email, string password)
    {
      try {
          var loginDto = new LoginReqDto { Email = email, Password = password };
          var response = await _httpClient.PostAsJsonAsync("auth/login", loginDto);

          if (response.IsSuccessStatusCode) {
              var tokenDto = await response.Content.ReadFromJsonAsync<TokenDto>();
              if (tokenDto != null) {
                  // ★ 핵심: 이후 모든 요청에 Bearer 토큰을 자동으로 붙임
                  _httpClient.DefaultRequestHeaders.Authorization = 
                      new AuthenticationHeaderValue("Bearer", tokenDto.AccessToken);
                  return true;
              }
          }
      } catch (Exception ex) {
          Console.WriteLine($"[Login Error] {ex.Message}");
      }
      return false;
    }


    // 폴링 : 서버에 해야할 일이 있는지 주기적으로 물어 봄
    public async Task<WorkOrderDto> PollWorkOrderAsync()
    {
        try
        {
            var url = $"api/mes/machine/poll?machineId={Uri.EscapeDataString(AppConfig.MachineId)}";

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
    public async Task<string> ReportProductionAsync(ProductionReportDto report)
    {
      try
      {
        var response = await _httpClient.PostAsJsonAsync("api/mes/machine/report", report);
        // 1. 성공 시 "OK" 반환
        if (response.IsSuccessStatusCode)
        {
            return "OK";
        }

        // 2. 실패 시 서버가 보낸 에러 메시지 확인
        // Spring Boot의 RuntimeException 메시지는 보통 응답 본문에 포함됩니다.
        string errorContent = await response.Content.ReadAsStringAsync();

        if (errorContent.Contains("SHORTAGE"))
        {
            return "SHORTAGE"; // 자재 부족 상태
        }

        return "SERVER_ERROR"; // 기타 서버 에러 (500 등)
        
      } catch (Exception ex)
      {
        Console.WriteLine($"[Error] 실적 보고 실패: {ex.Message}");
        return "NETWORK_ERROR";
      }
    }
}