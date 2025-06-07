using Asv.Common;
using Asv.IO;
using DroneControllerApp.DroneService;
using Microsoft.Extensions.Logging;

public class DroneController : IAsyncDisposable
{
    private readonly DroneFlightService _flight;
    private readonly DronePositionTracker _position;
    private readonly ILogger<DroneController> _logger;
    private readonly DroneConnectionService _connectionService;
    private IClientDevice? _drone;
    private CancellationTokenSource _cts = new();

    private IDisposable? _positionSubscription;

    public DroneController(
        ILogger<DroneController> logger,
        DroneConnectionService connectionService,
        DroneFlightService flightService,
        DronePositionTracker positionTracker)
    {
        _logger = logger;
        _connectionService = connectionService;
        _flight = flightService;
        _position = positionTracker;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _drone = await _connectionService.Connect(cancellationToken);
        _positionSubscription = _position.Subscribe(_drone);
    }

    public Task TakeOffAsync(double altitude, CancellationToken cancellationToken = default) =>
        _flight.TakeOff(_drone ?? throw new InvalidOperationException("Drone not connected"), altitude, cancellationToken);

    public Task FlyToAndLandAsync(GeoPoint target, CancellationToken cancellationToken = default) =>
        _flight.FlyToAndLand(_drone ?? throw new InvalidOperationException("Drone not connected"), target, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _cts.Dispose();
        _positionSubscription?.Dispose();

        if (_position is IAsyncDisposable asyncDisposablePosition)
            await asyncDisposablePosition.DisposeAsync();
        else
            _position.Dispose();

        // Если у _drone есть Dispose/DisposeAsync, можно вызвать тоже

        _logger.LogInformation("DroneController disposed");
    }
}
