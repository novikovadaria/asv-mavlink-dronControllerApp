using Asv.Cfg;
using Asv.IO;
using Asv.Mavlink;
using DroneConsoleApp.Logging;
using ObservableCollections;
using R3;


namespace DroneControllerApp.DroneService
{
    public class DroneConnectionService
    {
        private readonly ILoggerService _logger;
        private IDeviceExplorer? _deviceExplorer;
        private object? _router;

        public DroneConnectionService(ILoggerService logger)
        {
            _logger = logger;
        }

        public async Task<IClientDevice> Connect(CancellationToken cancel)
        {
            _logger.Info("Starting drone connection process...");

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

            _deviceExplorer = DeviceExplorer.Create(router, builder =>
            {
                builder.SetConfig(new ClientDeviceBrowserConfig
                {
                    DeviceTimeoutMs = 1000,
                    DeviceCheckIntervalMs = 30_000,
                });

                builder.Factories.RegisterDefaultDevices(identity, seq, new InMemoryConfiguration());
            });

            var drone = await DiscoverDrone(cancel);
            await WaitForReady(drone, cancel);
            await WaitForHeartbeat(drone, cancel);

            _logger.Info("Drone connection established.");
            return drone;
        }

        private async Task<IClientDevice> DiscoverDrone(CancellationToken cancel)
        {
            var tcs = new TaskCompletionSource<IClientDevice>();

            using var sub = _deviceExplorer!.Devices
                .ObserveAdd()
                .Take(1)
                .Subscribe(kvp => tcs.TrySetResult(kvp.Value.Value));

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            cts.CancelAfter(TimeSpan.FromSeconds(60));

            await using var reg = cts.Token.Register(() => tcs.TrySetCanceled());

            return await tcs.Task;
        }

        private async Task WaitForReady(IClientDevice drone, CancellationToken cancel)
        {
            var tcs = new TaskCompletionSource();

            using var sub = drone.State.Subscribe(state =>
            {
                if (state == ClientDeviceState.Complete)
                    tcs.TrySetResult();
            });

            using var reg = cancel.Register(() => tcs.TrySetCanceled());

            await tcs.Task;
        }

        private async Task WaitForHeartbeat(IClientDevice drone, CancellationToken cancel)
        {
            var heartbeat = drone.GetMicroservice<IHeartbeatClient>() ?? throw new Exception("No heartbeat client found");
            var tcs = new TaskCompletionSource();

            int count = 0;
            using var sub = heartbeat.RawHeartbeat
                .ThrottleLast(TimeSpan.FromMilliseconds(100))
                .Subscribe(_ =>
                {
                    if (++count >= 20)
                        tcs.TrySetResult();
                });

            using var reg = cancel.Register(() => tcs.TrySetCanceled());

            await tcs.Task;
        }
    }

}
