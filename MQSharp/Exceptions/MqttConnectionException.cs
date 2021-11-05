using System;

namespace MQSharp
{
    public class MqttConnectionException : Exception
    {
        public MqttConnectionException(byte errorCode)
            : base(GetExceptionMessage(errorCode))
        {
        }

        public MqttConnectionException(byte errorCode, Exception inner)
            : base(GetExceptionMessage(errorCode), inner)
        {
        }

        private static string GetExceptionMessage(byte errorCode)
        {
            switch (errorCode)
            {
                case 0x01:
                    return "0x01 Connection Refused, unacceptable protocol version. The Server does not support the level of the MQTT protocol requested by the Client";
                case 0x02:
                    return "0x02 Connection Refused, identifier rejected. The Client identifier is correct UTF-8 but not allowed by the Server";
                case 0x03:
                    return "0x03 Connection Refused, Server unavailable. The Network Connection has been made but the MQTT service is unavailable";
                case 0x04:
                    return "0x04 Connection Refused, bad user name or password. The data in the user name or password is malformed";
                case 0x05:
                    return "0x05 Connection Refused, not authorized. The Client is not authorized to connect";
                default:
                    return "Unknown exception with unknown return code";
            }
        }
    }
}
