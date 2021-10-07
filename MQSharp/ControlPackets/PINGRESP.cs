using MQSharp.Networking;
using System;
using System.Collections.Generic;
using System.Text;

namespace MQSharp.ControlPackets
{
    internal class PINGRESP : ControlPacket
    {
        public static PINGRESP Decode(MQTT_PROTOCOL_VERSION protocolVersion, NetworkTunnel networkTunnel)
        {
            PINGRESP pingrespPacket = new PINGRESP();

            int remainingLength = ControlPacket.DecodeRemainingLength(networkTunnel);

            return pingrespPacket;
        }
    }
}
