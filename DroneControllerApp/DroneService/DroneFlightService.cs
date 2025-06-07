using Asv.Common;
using Asv.IO;
using Asv.Mavlink;
using DroneConsoleApp.Logging;

namespace DroneControllerApp.DroneService
{
    public class DroneFlightService
    {
        private readonly ILoggerService _logger;

        public DroneFlightService(ILoggerService logger)
        {
            _logger = logger;
        }

        public async Task TakeOff(IClientDevice drone, double altitude, CancellationToken cancel)
        {
            var control = drone.GetMicroservice<ControlClient>() ?? throw new Exception("ControlClient not found");

            _logger.Info("Switching to GUIDED mode...");
            await control.SetGuidedMode(cancel);
            await Task.Delay(5000, cancel);

            _logger.Info($"Taking off to {altitude} meters...");
            await control.TakeOff(altitude, cancel);
            await Task.Delay(5000, cancel);
        }

        public async Task FlyToAndLand(IClientDevice drone, GeoPoint target, CancellationToken cancel)
        {
            var control = drone.GetMicroservice<ControlClient>() ?? throw new Exception("ControlClient not found");

            _logger.Info($"Flying to Lat={target.Latitude}, Lon={target.Longitude}, Alt={target.Altitude}");
            await control.SetGuidedMode(cancel);
            await control.GoTo(target, cancel);

            _logger.Info("Landing...");
            await control.DoLand(cancel);
        }
    }

}
