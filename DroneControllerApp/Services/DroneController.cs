using Asv.Cfg;
using Asv.Common;
using Asv.IO;
using Asv.Mavlink;
using DroneConsoleApp.Interfaces;
using DroneConsoleApp.Logging;
using ObservableCollections;
using R3;


namespace DroneConsoleApp.Services
{
    class DroneController : IDroneController, IDisposable
    {
        private object? _router;
        private IDeviceExplorer? _deviceExplorer;
        private IClientDevice? _drone;
        private ControlClient? _control;
        private CancellationTokenSource? _cts;
        private IDisposable? _positionSubscription;
        private readonly ILoggerService _logger;

        #region Constructor
        public DroneController(ILoggerService logger)
        {
            _logger = logger;
        }
        #endregion

        #region Configuration
        public async Task Connect()
        {
            _logger.Info("Starting drone connection process...");

            _cts = new CancellationTokenSource();

            var protocol = Protocol.Create(builder =>
            {
                builder.RegisterMavlinkV2Protocol();
                builder.Features.RegisterBroadcastFeature<MavlinkMessage>();
                builder.Formatters.RegisterSimpleFormatter();
            });

            var router = protocol.CreateRouter("ROUTER");
            router.AddTcpClientPort(p =>
            {
                p.Host = "127.0.0.1";
                p.Port = 5760;
            });

            _router = router;

            var seq = new PacketSequenceCalculator();
            var identity = new MavlinkIdentity(255, 255);

            _logger.Info("Initializing DeviceExplorer...");
            _deviceExplorer = DeviceExplorer.Create(router, builder =>
            {
                builder.SetConfig(new ClientDeviceBrowserConfig
                {
                    DeviceTimeoutMs = 1000,
                    DeviceCheckIntervalMs = 30_000,
                });

                builder.Factories.RegisterDefaultDevices(identity, seq, new InMemoryConfiguration());
            });

            await FindDrone();
            await WaitForDroneReady();
            await WaitForHeartbeat();

            _logger.Info("Drone connection established successfully.");
        }

        private async Task WaitForHeartbeat()
        {
            if (_drone == null)
                throw new InvalidOperationException("Drone not assigned");

            var heartbeat = _drone.GetMicroservice<IHeartbeatClient>();
            if (heartbeat == null)
                throw new Exception("No heartbeat client found");

            var tcs = new TaskCompletionSource();
            var count = 0;

            using var sub = heartbeat.RawHeartbeat
                .ThrottleLast(TimeSpan.FromMilliseconds(100))
                .Subscribe(p =>
                {
                    if (p == null) return;

                    if (++count >= 20)
                        tcs.TrySetResult();
                });

            await tcs.Task;
        }

        private async Task FindDrone()
        {
            if (_deviceExplorer == null)
                throw new InvalidOperationException("DeviceExplorer not initialized");

            _logger.Info("Searching for available drones...");

            var tcs = new TaskCompletionSource();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60), TimeProvider.System);
            await using var reg = cts.Token.Register(() =>
            {
                _logger.Error("Drone search timed out after 60 seconds.");
                tcs.TrySetCanceled();
            });

            using var sub = _deviceExplorer.Devices
                .ObserveAdd()
                .Take(1)
                .Subscribe(kvp =>
                {
                    _drone = kvp.Value.Value;
                    _logger.Info($"Drone found: Id={_drone.Id}");
                    tcs.TrySetResult();
                });

            try
            {
                await tcs.Task;
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Drone discovery timed out.");
            }

            if (_drone is null)
            {
                _logger.Error("Drone reference is null after detection.");
                throw new Exception("Drone not found");
            }

            _logger.Info("Drone successfully assigned.");
        }


        private async Task WaitForDroneReady()
        {
            if (_drone == null)
                throw new InvalidOperationException("Drone not assigned");

            var tcs = new TaskCompletionSource();

            using var sub = _drone.State
                .Subscribe(state =>
                {
                    if (state == ClientDeviceState.Complete)
                        tcs.TrySetResult();
                });

            await tcs.Task;
        }
        #endregion

        #region Commnands
        public async Task TakeOff(double altitude)
        {
            if (_drone == null)
                throw new InvalidOperationException("Drone is not connected");

            var cancel = _cts?.Token ?? CancellationToken.None;

            var control = _drone.GetMicroservice<ControlClient>()
                          ?? throw new Exception("ControlClient not found");

            _logger.Info("Switching to GUIDED mode...");
            await control.SetGuidedMode(cancel);
            await Task.Delay(TimeSpan.FromSeconds(5), cancel);

            _logger.Info($"Taking off to {altitude} meters...");
            await control.TakeOff(altitude, cancel);
            await Task.Delay(TimeSpan.FromSeconds(5), cancel);

            _logger.Info("Takeoff complete. Flying to target...");

            var position = _drone.GetMicroservice<IPositionClient>();
            var currentPos = position?.GlobalPosition.CurrentValue;

            double currentLat = currentPos?.Lat / 1_000_000.0 ?? 0;
            double currentLon = currentPos?.Lon / 1_000_000.0 ?? 0;
            double currentAlt = currentPos?.Alt ?? 0;

            _logger.Info($"Current position: Lat={currentLat:F6}, Lon={currentLon:F6}, Alt={currentAlt:F2} m");

            int latMicro = (int)(55.7558 * 1_000_000);
            int lonMicro = (int)(37.6173 * 1_000_000);

            var target = new GeoPoint(latMicro, lonMicro, currentAlt);
            _logger.Info($"Target position:  Lat={target.Latitude:F6}, Lon={target.Longitude:F6}, Alt={target.Altitude:F2} m");
        }

        public async Task FlyToAndLand(GeoPoint target, CancellationToken cancel)
        {
            if (_drone == null)
                throw new InvalidOperationException("Drone is not connected");

            var control = _drone.GetMicroservice<ControlClient>()
                          ?? throw new Exception("ControlClient not found");

            SubscribeToPosition();

            _logger.Info("Switching to GUIDED mode...");
            await control.SetGuidedMode(cancel);

            _logger.Info($"Flying to: Lat={target.Latitude}, Lon={target.Longitude}, Alt={target.Altitude}");
            await control.GoTo(target, cancel);

            _logger.Info("Reached target point.");
            _logger.Info("Landing...");
            await control.DoLand(cancel);
            _logger.Info("Landed.");
        }

        #endregion

        #region Subscribes
        private void SubscribeToPosition()
        {
            _positionSubscription?.Dispose();

            var position = _drone?.GetMicroservice<IPositionClient>();
            if (position == null)
            {
                _logger.Error("PositionClient not available. Skipping position subscription.");
                return;
            }

            _logger.Info("Subscribing to drone position updates...");

            _positionSubscription = position.GlobalPosition.Subscribe(pos =>
            {
                double latitude = pos.Lat / 1_000_000.0;
                double longitude = pos.Lon / 1_000_000.0;
                double altitude = pos.Alt / 1000.0;

                _logger.Info($"Updated position: Lat={latitude:F6}, Lon={longitude:F6}, Alt={altitude:F2} m");
            });
        }

        #endregion

        #region IDisposable Implementation  
        public void Dispose()
        {
            _positionSubscription?.Dispose();
            _deviceExplorer?.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();

            if (_router is IAsyncDisposable asyncDisposable)
            {
                asyncDisposable.DisposeAsync().AsTask().Wait();
            }
            else if (_router is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        #endregion
    }
}
