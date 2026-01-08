

// 생산 결과를 보고하는 DTO (백엔드 서버와 JSON으로 주고 받음)
public class ProductionReportDto
{
    public long OrderId { get; set; }
    public string? MachineId { get; set; }
    public string? Result { get; set; }
    public string? DefectCode { get; set; } 
}