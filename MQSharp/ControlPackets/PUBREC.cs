using MQSharp.Networking;
using System;
using System.Collections.Generic;
using System.Text;

namespace MQSharp.ControlPackets
{
    internal class PUBREC : ControlPacket
    {
        public int PacketId { get; set; }

        public PUBREC()
        {

        }

        public byte[] GetPacketData(MQTT_PROTOCOL_VERSION protocolVersion)
        {
            int fixedHeaderSize = 0;
            int variableHeaderSize = 0;
            int payloadSize = 0;
            int remainingLength = 0;
            byte[] packetData;
            int packetDataIndex = 0;

            // message identifier
            variableHeaderSize += 2;

            remainingLength += (variableHeaderSize + payloadSize);

            // first byte of fixed header
            fixedHeaderSize = 1;

            int temp = remainingLength;
            // increase fixed header size based on remaining length
            // (each remaining length byte can encode until 128)
            do
            {
                fixedHeaderSize++;
                temp = temp / 128;
            } while (temp > 0);

            // allocate buffer for message
            packetData = new byte[fixedHeaderSize + variableHeaderSize + payloadSize];

            // first fixed header byte
            packetData[packetDataIndex++] = CONTROL_PACKET_TYPE_PUBREC_BYTE;

            // encode remaining length
            packetDataIndex = this.EncodeRemainingLength(remainingLength, packetData, packetDataIndex);

            // get message identifier
            packetData[packetDataIndex++] = (byte)((this.PacketId >> 8) & 0x00FF); // MSB
            packetData[packetDataIndex++] = (byte)(this.PacketId & 0x00FF); // LSB 

            return packetData;
        }

        public static PUBREC Decode(byte fixedHeaderFirstByte, MQTT_PROTOCOL_VERSION protocolVersion, NetworkTunnel networkTunnel)
        {
            byte[] packetData;
            int packetDataIndex = 0;
            PUBREC pubrec = new PUBREC();

            // Check if last four reserve bits zero
            if ((fixedHeaderFirstByte & 0x0F) != 0x00)
            {
                throw new Exception("PUBACK reserve bits are none zero");
            }

            // get remaining length and allocate buffer
            int remainingLength = ControlPacket.DecodeRemainingLength(networkTunnel);
            packetData = new byte[remainingLength];

            // read bytes from socket...
            networkTunnel.ReceiveStream(packetData);

            // message id
            pubrec.PacketId = (ushort)((packetData[packetDataIndex++] << 8) & 0xFF00);
            pubrec.PacketId |= (packetData[packetDataIndex++]);

            return pubrec;
        }
    }
}
