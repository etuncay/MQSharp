using MQSharp.Networking;
using System;
using System.Collections.Generic;
using System.Text;

namespace MQSharp.ControlPackets
{
    internal abstract class ControlPacket
    {
        internal byte[] PROTOCOL_NAME_LENGTH_BYTES = new byte[] { 0x00, 0x04 }; // { Length MSB (0), Length LSB (4) }
        internal byte[] PROTOCOL_NAME_BYTES = new byte[] { 0x4D, 0x51, 0x54, 0x54 }; // { "M", "Q", "T", "T" }

        internal byte[] MQTT_3_1_1_PROTOCOL_LEVEL = new byte[] { 0x04 };
        internal byte[] MQTT_5_0_PROTOCOL_LEVEL = new byte[] { 0x05 };

        // Control packet type for fixed header
        internal const byte CONTROL_PACKET_TYPE_CONNECT_BYTE = 0x10; // 0001 0000
        internal static byte[] CONTROL_PACKET_TYPE_CONNECT = new byte[] { CONTROL_PACKET_TYPE_CONNECT_BYTE };

        internal const byte CONTROL_PACKET_TYPE_CONNACK_BYTE = 0x20; // 0010 0000
        internal static byte[] CONTROL_PACKET_TYPE_CONNACK = new byte[] { CONTROL_PACKET_TYPE_CONNACK_BYTE };

        internal const byte CONTROL_PACKET_TYPE_PINGREQ_BYTE = 0xC0; // 1100 0000
        internal static byte[] CONTROL_PACKET_TYPE_PINGREQ = new byte[] { CONTROL_PACKET_TYPE_PINGREQ_BYTE };

        internal const byte CONTROL_PACKET_TYPE_PINGRESP_BYTE = 0xD0; // 1101 0000
        internal static byte[] CONTROL_PACKET_TYPE_PINGRESP = new byte[] { CONTROL_PACKET_TYPE_PINGRESP_BYTE };

        internal const byte CONTROL_PACKET_TYPE_DISCONNECT_BYTE = 0xE0; // 1110 0000
        internal static byte[] CONTROL_PACKET_TYPE_DISCONNECT = new byte[] { CONTROL_PACKET_TYPE_DISCONNECT_BYTE };

        internal const byte CONTROL_PACKET_TYPE_SUBSCRIBE_BYTE = 0x82; // 1000 0010
        internal static byte[] CONTROL_PACKET_TYPE_SUBSCRIBE = new byte[] { CONTROL_PACKET_TYPE_SUBSCRIBE_BYTE };

        internal const byte CONTROL_PACKET_TYPE_SUBACK_BYTE = 0x90; // 1000 0010
        internal static byte[] CONTROL_PACKET_TYPE_SUBACK = new byte[] { CONTROL_PACKET_TYPE_SUBACK_BYTE };

        internal const byte CONTROL_PACKET_TYPE_UNSUBSCRIBE_BYTE = 0xA2; // 1010 0010
        internal static byte[] CONTROL_PACKET_TYPE_UNSUBSCRIBE = new byte[] { CONTROL_PACKET_TYPE_UNSUBSCRIBE_BYTE };

        internal const byte CONTROL_PACKET_TYPE_UNSUBACK_BYTE = 0xB0; // 1011 0000
        internal static byte[] CONTROL_PACKET_TYPE_UNSUBACK = new byte[] { CONTROL_PACKET_TYPE_UNSUBACK_BYTE };

        internal const byte CONTROL_PACKET_TYPE_PUBLISH_BYTE = 0x03; // 0000 0011
        internal const byte CONTROL_PACKET_TYPE_PUBACK_BYTE = 0x40; // 0100 0000
        internal const byte CONTROL_PACKET_TYPE_PUBREC_BYTE = 0x50; // 0101 0000
        internal const byte CONTROL_PACKET_TYPE_PUBREL_BYTE = 0x62; // 0110 0010
        internal const byte CONTROL_PACKET_TYPE_PUBCOMP_BYTE = 0x70; // 0111 0000

        protected int EncodeRemainingLength(int remainingLength, byte[] data, int index)
        {
            int X = 0;
            do
            {
                X = remainingLength % 128;
                remainingLength /= 128;
                if (remainingLength > 0)
                    X = X | 0x80;
                data[index++] = (byte)X;
            } while (remainingLength > 0);
            return index;
        }

        protected static int DecodeRemainingLength(NetworkTunnel networkTunnel)
        {
            int multiplier = 1;
            int value = 0;
            int digit = 0;
            byte[] nextByte = new byte[1];
            do
            {
                // next digit from stream
                networkTunnel.ReceiveStream(nextByte);
                digit = nextByte[0];
                value += ((digit & 127) * multiplier);
                multiplier *= 128;
            } while ((digit & 128) != 0);
            return value;
        }
    }
}
