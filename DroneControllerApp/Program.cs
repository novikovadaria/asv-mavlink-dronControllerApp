using Asv.Common;
using DroneConsoleApp.Logging;
using DroneConsoleApp.Services;

class Program
{
    static async Task Main(string[] args)
    {
        ILoggerService logger = new ConsoleLoggerService();

        using var mission = new MissionController(logger);

        try
        {
            GeoPoint target = new GeoPoint(
                (int)(55.7558 * 1_000_000),
                (int)(37.6173 * 1_000_000),
                20
            );

            await mission.RunMission(20.0, target);
        }
        catch (Exception ex)
        {
            logger.Error("Mission failed in Main method", ex);
            Console.WriteLine($"Mission failed: {ex.Message}");
        }
    }
}