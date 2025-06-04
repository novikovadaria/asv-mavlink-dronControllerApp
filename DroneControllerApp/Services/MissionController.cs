using Asv.Common;
using DroneConsoleApp.Interfaces;
using DroneConsoleApp.Logging;
namespace DroneConsoleApp.Services
{
    public class MissionController : IDisposable
    {
        private readonly IDroneController _drone;
        private readonly ILoggerService _logger;

        public MissionController(ILoggerService logger)
        {
            _logger = logger;
            _drone = new DroneController(_logger);
        }

        public async Task RunMission(double altitude, GeoPoint destination)
        {
            try
            {
                _logger.MissionStart("Ardu SITL Test Mission");

                await _drone.Connect();

                await _drone.TakeOff(altitude);

                await _drone.FlyToAndLand(destination, CancellationToken.None);

                _logger.Info("Mission complete: Drone reached destination and landed.");
            }
            catch (Exception ex)
            {
                _logger.Error("Mission failed.", ex);
                throw;
            }
        }

        public void Dispose()
        {
            if (_drone is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

}
