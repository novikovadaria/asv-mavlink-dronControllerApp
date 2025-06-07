using Asv.IO;
using Asv.Mavlink;
using DroneControllerApp.RouterFactory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DroneControllerApp.RouterService
{
    public static class RouterFactory
    {
        public static IProtocolRouter CreateRouter()
        {
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

            return router;
        }
    }

}
