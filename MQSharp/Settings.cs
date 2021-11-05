using System;
using System.Collections.Generic;
using System.Text;

namespace MQSharp
{
    internal class Settings
    {
        public static int MAX_KEEP_ALIVE_PERIOD { get; } = 65535; // the maximum can hold by 16 bit
        public static int DEFAULT_TIMEOUT { get; } = 30000; // default timeout period
        public static int MIN_TOPIC_LENGTH = 1; // minimum topics list length
        public static int MAX_TOPIC_LENGTH = 65535; // maximum topic list length
        public static int MAX_PACKET_ID = 65535;

        public static Dictionary<byte, string> MQTT_3_1_1_CONNACK_RETURN_CODE = new Dictionary<byte, string>
        {
            { 0x00, "Connection Accepted" },
            { 0x01, "Connection Refused, unacceptable protocol version. The Server does not support the level of the MQTT protocol requested by the Client" },
            { 0x02, "Connection Refused, identifier rejected. The Client identifier is correct UTF-8 but not allowed by the Server" },
            { 0x03, "Connection Refused, Server unavailable. The Network Connection has been made but the MQTT service is unavailable" },
            { 0x04, "Connection Refused, bad user name or password. The data in the user name or password is malformed" },
            { 0x05, "Connection Refused, not authorized. The Client is not authorized to connect" }

        };

        public static Dictionary<byte, string> MQTT_3_1_1_SUBACK_RETURN_CODE = new Dictionary<byte, string>
        {
            { 0x00, "Success - Maximum QoS 0" },
            { 0x01, "Success - Maximum QoS 1" },
            { 0x02, "Success - Maximum QoS 2" },
            { 0x80, "Failure" }
        };
    }
}
