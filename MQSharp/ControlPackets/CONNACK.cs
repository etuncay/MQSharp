using MQSharp.Exceptions;
using MQSharp.Networking;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace MQSharp.ControlPackets
{
    internal class CONNACK : ControlPacket
    {
        #region Properties >>> 

        public byte ReturnCode { get; set; }

        public bool Accepted { get; set; } = false;

        public bool SessionPresent { get; set; } = false;

        #endregion

        #region Methods >>> 

        public static CONNACK Decode(MQTT_PROTOCOL_VERSION protocolVersion, NetworkTunnel networkTunnel)
        {
            byte[] packetData;
            CONNACK connackPacket = new CONNACK();

            if (protocolVersion == MQTT_PROTOCOL_VERSION.MQTT_3_1_1)
            {
                int remainingLength = ControlPacket.DecodeRemainingLength(networkTunnel);
                packetData = new byte[remainingLength];

                networkTunnel.ReceiveStream(packetData);

                if (packetData[0] == 0x01) // 0000 0001, session present
                {
                    connackPacket.SessionPresent = true;
                }
                else
                {
                    connackPacket.SessionPresent = false;
                }

                if (packetData[1] == 0x00) // 0000 0000, if success return code
                {
                    connackPacket.Accepted = true;
                }
                else
                {
                    connackPacket.Accepted = false;
                }

                connackPacket.ReturnCode = packetData[1];

            }
            else if(protocolVersion == MQTT_PROTOCOL_VERSION.MQTT_5_0)
            {

            }

            return connackPacket;
        }

        #endregion
    }
}
