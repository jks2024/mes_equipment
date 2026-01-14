using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;

public class ApiService
{
    private readonly HttpClient _httpClient; 
    private TokenDto _currentTokens;

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(AppConfig.BaseUrl)
        };
    }

    // 1. 로그인 기능
    public async Task<bool> LoginAsync(string email, string password)
    {
        try {
            var loginDto = new LoginReqDto { Email = email, Password = password };
            var response = await _httpClient.PostAsJsonAsync("auth/login", loginDto);

            if (response.IsSuccessStatusCode) {
                _currentTokens = await response.Content.ReadFromJsonAsync<TokenDto>();
                if (_currentTokens != null) {
                    SetAuthHeader(_currentTokens.AccessToken);
                    Console.WriteLine("[Auth] 로그인 성공 및 토큰 저장 완료");
                    return true;
                }
            }
        } catch (Exception ex) {
            Console.WriteLine($"[Login Error] {ex.Message}");
        }
        return false;
    }

    // 2. 토큰 갱신 기능 (서버의 @PostMapping("/refresh")에 대응)
    private async Task<bool> RefreshTokenAsync()
    {
        try {
            if (_currentTokens == null || string.IsNullOrEmpty(_currentTokens.RefreshToken)) return false;

            // 로그에 찍힌 에러(HttpMediaTypeNotSupportedException)를 해결하기 위해 
            // 다시 JSON 방식으로 보냅니다.
            var response = await _httpClient.PostAsJsonAsync("auth/refresh", _currentTokens);

            if (response.IsSuccessStatusCode) {
                _currentTokens = await response.Content.ReadFromJsonAsync<TokenDto>();
                if (_currentTokens != null) {
                    SetAuthHeader(_currentTokens.AccessToken);
                    Console.WriteLine("[Auth] 토큰 재발급 성공");
                    return true;
                }
            } else {
                // 실패 시 원인 파악을 위한 로그
                string error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[Refresh Failed] 상태코드: {response.StatusCode}, 사유: {error}");
            }
        } catch (Exception ex) { 
            Console.WriteLine($"[Refresh Error] {ex.Message}"); 
        }
        return false;
    }

    // 3. 공통 요청 처리 (401 발생 시 자동 재발급 및 재시도)
    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> action)
    {
        var response = await action();

        // 401(Unauthorized)이 발생했다면 토큰이 만료된 것임
        if (response.StatusCode == HttpStatusCode.Unauthorized) {
            Console.WriteLine("[Auth] 토큰 만료 감지 -> 재발급 시도...");
            
            if (await RefreshTokenAsync()) {
                // 재발급 성공했으므로 실패했던 action을 한 번 더 시도
                Console.WriteLine("[Auth] 재발급 성공 -> 기존 요청 재시도");
                return await action();
            }
        }
        return response;
    }

    // 4. 폴링
    public async Task<WorkOrderDto> PollWorkOrderAsync()
    {
        try {
            var url = $"api/mes/machine/poll?machineId={Uri.EscapeDataString(AppConfig.MachineId)}";
            
            // SendWithRetryAsync를 거쳐서 호출
            var response = await SendWithRetryAsync(() => _httpClient.GetAsync(url));

            if (response.StatusCode == HttpStatusCode.OK) {
                return await response.Content.ReadFromJsonAsync<WorkOrderDto>();
            }
        } catch (Exception ex) {
            Console.WriteLine($"[Error] API 통신 실패 : {ex.Message}");
        }
        return null;
    }

    // 5. 생산 실적 보고
    public async Task<string> ReportProductionAsync(ProductionReportDto report)
    {
        try {
            // SendWithRetryAsync를 거쳐서 호출
            var response = await SendWithRetryAsync(() => _httpClient.PostAsJsonAsync("api/mes/machine/report", report));

            if (response.IsSuccessStatusCode) {
                return "OK";
            }

            string errorContent = await response.Content.ReadAsStringAsync();
            return errorContent.Contains("SHORTAGE") ? "SHORTAGE" : "SERVER_ERROR";
        } catch (Exception ex) {
            Console.WriteLine($"[Error] 실적 보고 실패: {ex.Message}");
            return "NETWORK_ERROR";
        }
    }

    private void SetAuthHeader(string token) {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    // 실시간 설비 상태 온도 보고 
    public async Task ReportMachineStatusAsync(MachineStatusDto statusDto)
    {
        try
        {
            // 실시간 모니터링 API 호출
            var response = await _httpClient.PostAsJsonAsync("api/mes/machine/status", statusDto);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Status Error] 서버 응답 오류: {response.StatusCode}");
            }
        } catch (Exception ex)
        {
            Console.WriteLine($"[Status Error] 설비 상태 전송 실패: {ex.Message}");
        }
    }
}