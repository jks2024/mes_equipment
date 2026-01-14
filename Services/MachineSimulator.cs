using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class MachineSimulator
{
    private readonly ApiService _apiService;  // Backend Server 통신을 위해 주입 받음
    private readonly TcpClientService _tcpService;  // 장비와 연결하기 위해 주입 받음
    private WorkOrderDto? _currentWorkOrder;

    public MachineSimulator (ApiService apiService, TcpClientService tcpService)
    {
        _apiService = apiService;  // 외부에서 만들어진 객체를 주입 받음
        _tcpService = tcpService;
    }

    public async Task RunAsync()
    {
        Console.WriteLine($"🚀 수집기 가동 시작....");

        // 장비 연결 보장
        await ConnectToDeviceAsync();

        // 실시간 장비 데이터 수집 (TCP/IP)
        _ = Task.Run(async () => await ReceiveDeviceDataLoop());

        while (true)
        {
            WorkOrderDto? workOrder = null;

            try
            {
                workOrder = await _apiService.PollWorkOrderAsync(); // 서버에 생산 지시가 있는지 확인
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Poll Error] {ex.Message}");
                await Task.Delay(AppConfig.PollingIntervalMs);
                continue;
            }

            if (workOrder != null)
            {
                _currentWorkOrder = workOrder;
                Console.WriteLine($"작업 수주 : {workOrder.ProductCode} / 진행:{workOrder.CurrentQty}/{workOrder.TargetQty} / 상태:{workOrder.Status}");
                await SendWorkOrderToDevice(workOrder);

                await Task.Delay(AppConfig.PollingIntervalMs);
            }
            else
            {
                Console.WriteLine("[-] 현재 할당된 작업이 없습니다.");
                await Task.Delay(AppConfig.PollingIntervalMs); // ✅ 무한 초고속 폴링 방지
            }
        }
    }

    private async Task ConnectToDeviceAsync()
    {
        while(!_tcpService.IsConnected)
        {
            try
            {
                await _tcpService.ConnectAsync("127.0.0.1", 5006);
                Console.WriteLine("L1 장비 연결 성공");
            } catch
            {
                Console.WriteLine("L1 연결 실패. 재 시도 중....");
                await Task.Delay(3000);
            }
        }
    }

    private async Task ReceiveDeviceDataLoop()
    {
        while (true)
        {
            if (!_tcpService.IsConnected)
            {
                await ConnectToDeviceAsync();
                continue;
            }
            byte[] packet = await _tcpService.ReadPacketAsync(7);
            if (packet == null || packet[0] != 0x02) continue;

            byte msgType = packet[1];
            int val = BitConverter.ToInt32(packet, 2);  // 2번부터 4바이트를 읽어 32bit 정수로 변환
            if (msgType == 0x10) await HandleTemerature(val); // 채널 A
            else if (msgType == 0x20) await HandleProductionResult(val); // 채널 B
        }
    }

    private async Task HandleTemerature(int temp)
    {
        var status = new MachineStatusDto { 
            MachineId = AppConfig.MachineId, 
            Temperature = temp 
        };

        // 비동기로 전송 (백그라운드 처리)
       // _ = Task.Run(() => _apiService.ReportMachineStatusAsync(status));

        if (temp >= 80) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"⚠️ [과열 경고] {temp}℃");
            Console.ResetColor();
        }
    }

    private async Task HandleProductionResult(int result)
    {
        if (_currentWorkOrder == null)
        {
            Console.WriteLine("[WARN] 현재 작업 지시가 없어 생산 결과를 처리할 수 없습니다.");
            return;
        }

        string serialNo =
            $"{_currentWorkOrder.ProductCode}-" +
            $"{DateTime.Now:yyyyMMdd}-" +
            $"{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

        var report = new ProductionReportDto
        {
            OrderId = _currentWorkOrder.Id,
            MachineId = AppConfig.MachineId,
            Result = (result == 1) ? "OK" : "NG",
            SerialNo = serialNo
        };

        string status = await _apiService.ReportProductionAsync(report);
        Console.WriteLine($"생산 보고 : {report.Result}");
    }

    private async Task SendWorkOrderToDevice(WorkOrderDto order)
    {
        if (!_tcpService.IsConnected) return;
        byte[] packet = new byte[7];
        packet[0] = 0x02;  // STX
        packet[1] = 0x20;  // 생산 작업 지시
        Array.Copy(BitConverter.GetBytes(order.TargetQty), 0, packet, 2, 4);
        packet[6] = 0x03;
        await _tcpService.SendAsync(packet);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[CMD] 설비에 작업 지시 전달 → 목표 수량: {order.TargetQty}");
        Console.ResetColor();
    }
}