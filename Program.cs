
class Program
{
    static async Task Main()
    {
        var apiService = new ApiService();
        var simulator = new MachineSimulator(apiService);

        try
        {
            await simulator.RunAsync();
        } catch (Exception ex)
        {
            Console.WriteLine($"심각한 오류 발생 : {ex.Message}");
        }
    }
}