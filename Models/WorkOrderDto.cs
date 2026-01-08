
// 작업 지시 관련 해서 서버로 부터 받는 포맷
public class WorkOrderDto
{
    public long Id { get; set; }
    public string ProductCode { get; set; }
    public int TargetQty { get; set; }
    public int CurrentQty { get; set; }
    public string Status { get; set; }
}