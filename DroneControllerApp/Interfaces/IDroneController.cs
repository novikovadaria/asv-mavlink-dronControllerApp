using Asv.Common;

namespace DroneConsoleApp.Interfaces
{
    public interface IDroneController : IDisposable
    {
        Task Connect();
        Task TakeOff(double altitude);
        Task FlyToAndLand(GeoPoint target, CancellationToken cancel);
    }

}
