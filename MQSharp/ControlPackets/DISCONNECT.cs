using System;
using System.Collections.Generic;
using System.Text;

namespace MQSharp.ControlPackets
{
    internal class DISCONNECT : ControlPacket
    {
        private MQTT_PROTOCOL_VERSION _protocolVersion;

        public DISCONNECT(MQTT_PROTOCOL_VERSION protocolVersion)
        {
            _protocolVersion = protocolVersion;
        }

        public byte[] GetPacketData()
        {
            byte[] packetData = new byte[2];
            int packetDataIndex = 0;

            if(_protocolVersion == MQTT_PROTOCOL_VERSION.MQTT_3_1_1)
            {
                Array.Copy(CONTROL_PACKET_TYPE_DISCONNECT, 0, packetData, packetDataIndex, CONTROL_PACKET_TYPE_DISCONNECT.Length);
                packetDataIndex += CONTROL_PACKET_TYPE_DISCONNECT.Length;
                packetData[packetDataIndex] = 0x00;
            }
            else if(_protocolVersion == MQTT_PROTOCOL_VERSION.MQTT_5_0)
            {

            }

            return packetData;
        }
    }
}
