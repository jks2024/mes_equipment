using System;
using System.Threading.Tasks;

public class MachineSimulator
{
    private readonly ApiService _apiService;
    private readonly Random _random = new Random();

    public MachineSimulator (ApiService apiService)
    {
        _apiService = apiService;  // 외부에서 만들어진 객체를 주입 받음
    }

    public async Task RunAsync()
    {
        Console.WriteLine($"🚀 설비 [{AppConfig.MachineId}] 가동을 시작합니다.");

        while (true) {
            Console.WriteLine("\n[Poller] 작업 지시를 확인 중......");
            var workOrder = await _apiService.PollWorkOrderAsync();  // 서버에 생산 지시가 있는지 확인

            if (workOrder != null) // 새로운 작업이 있음
            {
                // 작업 수행 코드
                
            } else
            {
                Console.WriteLine("[-] 현재 할당된 작업이 없습니다.");
            }

            await Task.Delay(AppConfig.PollingIntervalMs); // 5초 지연 이후 반복 수행
            
        }        
    }

    private async Task ProcessWorkOrder (WorkOrderDto order)
    {
        // 방어 코드: 완료된 작업이면 생산 금지
        if (order.Status == "COMPLETED")
        {
            Console.WriteLine("[SKIP] 이미 완료된 작업입니다.");
            return;
        }

        // 생산 시뮬레이션
        // 터미널 글자 색상 변경
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[Active] 작업 수주: {order.ProductCode} (목표: {order.TargetQty})");
        Console.ResetColor();

        await Task.Delay(2000);  // 2초 마다 생산
        bool isSuccess = _random.NextDouble() > 0.05; // 95% 확률로 양품

        var report = new ProductionReportDto
        {
            OrderId = order.Id,
            MachineId = AppConfig.MachineId,
            Result = isSuccess ? "OK" : "NG",
            DefectCode = isSuccess ? null : "ERR-102" // 불량인 경우 불량 코드 추가
        };

        bool reportResult = await _apiService.ReportProductionAsync(report);
        if (reportResult)
        {
            Console.ForegroundColor = isSuccess ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"[Report] {order.ProductCode} 생산 결과: {report.Result}");
            Console.ResetColor();
        } else
        {
            Console.WriteLine("[Warn] 보고 실패. 다음 폴링에서 재시도합니다.");
        }        
    }
}