using MQSharp.Networking;
using System;
using System.Collections.Generic;
using System.Text;

namespace MQSharp.ControlPackets
{
    internal class SUBACK : ControlPacket
    {
        private byte[] _returnCodes;

        #region Properties >>>

        public int PacketId { get; set; }

        public byte[] ReturnCodes
        {
            get
            {
                return this._returnCodes;
            }
            set
            {
                this._returnCodes = value;
            }
        }

        #endregion


        public static SUBACK Decode(MQTT_PROTOCOL_VERSION protocolVersion, NetworkTunnel networkTunnel)
        {
            byte[] packetData;
            int packetDataIndex = 0;
            SUBACK suback = new SUBACK();

            if(protocolVersion == MQTT_PROTOCOL_VERSION.MQTT_3_1_1)
            {
                // get remaining length and allocate buffer
                int remainingLength = ControlPacket.DecodeRemainingLength(networkTunnel);
                packetData = new byte[remainingLength];

                // read bytes from socket...
                networkTunnel.ReceiveStream(packetData);

                // message id
                suback.PacketId = (ushort)((packetData[packetDataIndex++] << 8) & 0xFF00);
                suback.PacketId |= (packetData[packetDataIndex++]);

                // payload contains QoS levels granted
                suback._returnCodes = new byte[remainingLength - 2];
                int qosIdx = 0;
                do
                {
                    suback._returnCodes[qosIdx++] = packetData[packetDataIndex++];
                } while (packetDataIndex < remainingLength);
            }
            else if(protocolVersion == MQTT_PROTOCOL_VERSION.MQTT_5_0)
            {

            }
            
            return suback;
        }

    }
}
