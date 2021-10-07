using System;
using System.Collections.Generic;
using System.Text;

namespace MQSharp.ControlPackets
{
    internal class PINGREQ : ControlPacket
    {

        public byte[] GetPacketData()
        {
            int packetDataIndex = 0;
            byte[] packetData = new byte[2];

            Array.Copy(CONTROL_PACKET_TYPE_PINGREQ, 0, packetData, packetDataIndex, CONTROL_PACKET_TYPE_PINGREQ.Length);
            packetDataIndex += CONTROL_PACKET_TYPE_PINGREQ.Length;
            packetData[packetDataIndex] = 0x00;

            return packetData;
        }
    }
}
