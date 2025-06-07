using Asv.IO;
using Asv.Mavlink;
using Asv.Mavlink.Common;
using DroneConsoleApp.Logging;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DroneControllerApp.DroneService
{
    public class DronePositionTracker 
    {
        private readonly ILoggerService _logger;
        private IDisposable? _subscription;

        public DronePositionTracker(ILoggerService logger)
        {
            _logger = logger;
        }

        public void Subscribe(IClientDevice drone)
        {
            var position = drone.GetMicroservice<IPositionClient>();
            if (position == null)
            {
                _logger.Error("PositionClient not found.");
                return;
            }

            _subscription?.Dispose();

            _subscription = position.GlobalPosition.Subscribe(new Observer<GlobalPositionIntPayload?>(pos =>
            {
                if (pos == null) return;

                double lat = pos.Lat / 1_000_000.0;
                double lon = pos.Lon / 1_000_000.0;
                double alt = pos.Alt / 1000.0;

                _logger.Info($"Position update: Lat={lat:F6}, Lon={lon:F6}, Alt={alt:F2}");
            }));
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }

}
