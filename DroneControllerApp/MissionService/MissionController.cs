using Asv.Common;
using Microsoft.Extensions.Logging;

public class MissionController : IDisposable
{
    private readonly DroneController _drone;
    private readonly ILogger<MissionController> _logger;

    public MissionController(DroneController drone, ILogger<MissionController> logger)
    {
        _drone = drone;
        _logger = logger;
    }

    public async Task RunMission(double altitude, GeoPoint destination)
    {
        try
        {
            _logger.LogInformation("Mission started");

            await _drone.StartAsync();

            await _drone.TakeOffAsync(altitude);

            await _drone.FlyToAndLandAsync(destination);

            _logger.LogInformation("Mission complete: Drone reached destination and landed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mission failed");
            throw;
        }
    }

    public void Dispose()
    {
        _drone.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
