using MQSharp.Networking;
using System;
using System.Collections.Generic;
using System.Text;

namespace MQSharp.ControlPackets
{
    internal class UNSUBACK : ControlPacket
    {
        public int PacketId { get; set; }

        public static UNSUBACK Decode(MQTT_PROTOCOL_VERSION protocolVersion, NetworkTunnel networkTunnel)
        {
            byte[] packetData;
            int packetDataIndex = 0;
            UNSUBACK puback = new UNSUBACK();

            if (protocolVersion == MQTT_PROTOCOL_VERSION.MQTT_3_1_1)
            {

                // get remaining length and allocate buffer
                int remainingLength = ControlPacket.DecodeRemainingLength(networkTunnel);
                packetData = new byte[remainingLength];

                // read bytes from socket...
                networkTunnel.ReceiveStream(packetData);

                // message id
                puback.PacketId = (ushort)((packetData[packetDataIndex++] << 8) & 0xFF00);
                puback.PacketId |= (packetData[packetDataIndex++]);
            }
            else if (protocolVersion == MQTT_PROTOCOL_VERSION.MQTT_5_0)
            {

            }

            return puback;
        }

    }
}
