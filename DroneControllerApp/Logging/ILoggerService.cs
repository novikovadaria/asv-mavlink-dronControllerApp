namespace DroneConsoleApp.Logging
{
    public interface ILoggerService
    {
        void Info(string message);
        void Error(string message, Exception? ex = null);
        void MissionStart(string missionName);
        void MissionEnd(string missionName);
    }
}
