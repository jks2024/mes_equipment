using System;
using System.Threading.Tasks;

// static으로 만들면 객체 생성 없이 바로 호출할 수 있어 편리합니다.
public static class LoginConsole
{
    public static async Task<bool> AttemptLogin(ApiService apiService)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("================================================");
        Console.WriteLine("     K-Smart MES 설비 시뮬레이터 로그인         ");
        Console.WriteLine("================================================");
        Console.ResetColor();

        Console.Write(" 📧 이메일: ");
        string email = Console.ReadLine() ?? "";

        Console.Write(" 🔑 비밀번호: ");
        string password = ReadPassword(); // 비밀번호 마스킹 처리

        Console.WriteLine("\n------------------------------------------------");
        Console.WriteLine(" 서버 인증 중... 잠시만 기다려주세요.");

        // ApiService를 통해 실제 로그인 시도
        bool isSuccess = await apiService.LoginAsync(email, password);

        if (isSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" ✅ 인증 성공! 가동 모드로 진입합니다.");
            Console.ResetColor();
            await Task.Delay(1000);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" ❌ 인증 실패: 이메일 또는 비밀번호를 확인하세요.");
            Console.ResetColor();
        }
        
        return isSuccess;
    }

    // 비밀번호 입력 시 화면에 *로 표시하는 유틸리티
    private static string ReadPassword()
    {
        string pass = "";
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
            {
                pass = pass.Remove(pass.Length - 1);
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                pass += key.KeyChar;
                Console.Write("*");
            }
        }
        return pass;
    }
}