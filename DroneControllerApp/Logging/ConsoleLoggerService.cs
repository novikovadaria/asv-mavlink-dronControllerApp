
namespace DroneConsoleApp.Logging
{
    public class ConsoleLoggerService : ILoggerService
    {
        public void Info(string message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} — {message}");
            Console.ResetColor();
        }

        public void Error(string message, Exception? ex = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} — {message}");
            if (ex != null)
            {
                Console.WriteLine($"       Exception: {ex.Message}");
            }
            Console.ResetColor();
        }

        public void MissionStart(string missionName)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[MISSION START] {DateTime.Now:HH:mm:ss} — {missionName}");
            Console.ResetColor();
        }

        public void MissionEnd(string missionName)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[MISSION END]   {DateTime.Now:HH:mm:ss} — {missionName}");
            Console.ResetColor();
        }
    }
}
