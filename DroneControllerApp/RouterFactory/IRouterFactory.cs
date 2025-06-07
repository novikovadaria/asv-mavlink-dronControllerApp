using Asv.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DroneControllerApp.RouterFactory
{
    public interface IRouterFactory
    {
        IProtocolRouter CreateRouter();
    }
}
