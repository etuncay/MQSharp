using System;
using System.Collections.Generic;
using System.Text;

namespace MQSharp.ControlPackets
{
    internal class CONNECT : ControlPacket
    {
        #region Variables...

        #endregion

        #region Properties...

        public byte[] ClientId { get; set; }
        public byte[] Username { get; set; }
        public byte[] Password { get; set; }
        public bool WillFlag { get; set; }
        public byte[] WillTopic { get; set; }
        public byte[] WillMessage { get; set; }
        public bool WillRetain { get; set; }
        public QoS_Level WillQoSLevel { get; set; }
        public bool CleanSession { get; set; }
        public int KeepAlivePeriod { get; set; }
        public MQTT_PROTOCOL_VERSION MqttProtocolVersion { get; set; }

        #endregion

        #region Constructors...

        public CONNECT(
            string clientId,
            string username,
            string password,
            bool willFlag,
            string willTopic,
            string willMessage,
            bool willRetain,
            QoS_Level willQoSLevel,
            bool cleanSession,
            int keepAlivePeriod,
            MQTT_PROTOCOL_VERSION mqttProtocolVersion)
        {
            this.ClientId = clientId != null ? Encoding.UTF8.GetBytes(clientId) : Encoding.UTF8.GetBytes(string.Empty);
            this.Username = (!string.IsNullOrEmpty(username) && !string.IsNullOrWhiteSpace(username)) ? Encoding.UTF8.GetBytes(username) : null;
            this.Password = (!string.IsNullOrEmpty(password) && !string.IsNullOrWhiteSpace(password)) ? Encoding.UTF8.GetBytes(password) : null;
            this.WillFlag = willFlag;
            this.WillTopic = (this.WillFlag && !string.IsNullOrEmpty(willTopic) && !string.IsNullOrWhiteSpace(willTopic)) ? Encoding.UTF8.GetBytes(willTopic) : null; ;
            this.WillMessage = (this.WillFlag && !string.IsNullOrEmpty(willMessage) && !string.IsNullOrWhiteSpace(willMessage)) ? Encoding.UTF8.GetBytes(willMessage) : null; ;
            this.WillRetain = willRetain;
            this.WillQoSLevel = willQoSLevel;
            this.CleanSession = cleanSession;
            this.KeepAlivePeriod = keepAlivePeriod;
            this.MqttProtocolVersion = mqttProtocolVersion;
        }

        #endregion


        public byte[] GetPacketData()
        {
            byte[] packetData;
            int packetDataIndex = 0;

            int fixedHeaderByteLength = 0;
            int variableHeaderByteLength = 0;
            int payloadByteLength = 0;
            int remainingLength = 0;

            // will flag set, will topic and will message MUST be present
            if (!this.ValidWill(this.WillTopic, this.WillMessage, this.WillFlag, this.WillQoSLevel, this.WillRetain))
            {
                //throw new MqttClientException(MqttClientErrorCode.WillWrong);
                throw new Exception("Wrong Will");
            }

            if (this.KeepAlivePeriod > Settings.MAX_KEEP_ALIVE_PERIOD)
            {
                //throw new MqttClientException(MqttClientErrorCode.KeepAliveWrong);
                throw new Exception("Wrong Keep Alive Period");
            }

            // protocol name field size
            variableHeaderByteLength += (PROTOCOL_NAME_LENGTH_BYTES.Length + PROTOCOL_NAME_BYTES.Length);

            if (this.MqttProtocolVersion == MQTT_PROTOCOL_VERSION.MQTT_3_1_1)
            {
                // [v3.1.1]
                // protocol level field size
                variableHeaderByteLength += MQTT_3_1_1_PROTOCOL_LEVEL.Length;
                // connect flags field size = 1 byte
                variableHeaderByteLength += 1;

            }
            else if(this.MqttProtocolVersion == MQTT_PROTOCOL_VERSION.MQTT_5_0)
            {
                // [v5.0]
                // protocol level field size
                variableHeaderByteLength += MQTT_5_0_PROTOCOL_LEVEL.Length;
                // connect flags field size = 1 byte
                variableHeaderByteLength += 1;
            }

            // keep alive timer field size = 2
            variableHeaderByteLength += 2;

            // client identifier field size
            payloadByteLength += this.ClientId.Length + 2;
            // will topic field size
            payloadByteLength += (this.WillTopic != null) ? (this.WillTopic.Length + 2) : 0;
            // will message field size
            payloadByteLength += (this.WillMessage != null) ? (this.WillMessage.Length + 2) : 0;
            // username field size
            payloadByteLength += (this.Username != null) ? (this.Username.Length + 2) : 0;
            // password field size
            payloadByteLength += (this.Password != null) ? (this.Password.Length + 2) : 0;

            remainingLength += (variableHeaderByteLength + payloadByteLength);

            // first byte of fixed header
            fixedHeaderByteLength = CONTROL_PACKET_TYPE_CONNECT.Length;

            // increase fixed header size based on remaining length
            // (each remaining length byte can encode until 128)
            int temp = remainingLength;

            do
            {
                fixedHeaderByteLength++;
                temp = temp / 128;
            } while (temp > 0);

            // allocate buffer for message
            packetData = new byte[fixedHeaderByteLength + variableHeaderByteLength + payloadByteLength];

            // first fixed header byte
            Array.Copy(CONTROL_PACKET_TYPE_CONNECT, 0, packetData, packetDataIndex, CONTROL_PACKET_TYPE_CONNECT.Length);
            packetDataIndex += CONTROL_PACKET_TYPE_CONNECT.Length;

            // encode remaining length
            packetDataIndex = this.EncodeRemainingLength(remainingLength, packetData, packetDataIndex);

            // protocol name
            Array.Copy(PROTOCOL_NAME_LENGTH_BYTES, 0, packetData, packetDataIndex, PROTOCOL_NAME_LENGTH_BYTES.Length);
            packetDataIndex += PROTOCOL_NAME_LENGTH_BYTES.Length;
            Array.Copy(PROTOCOL_NAME_BYTES, 0, packetData, packetDataIndex, PROTOCOL_NAME_BYTES.Length);
            packetDataIndex += PROTOCOL_NAME_BYTES.Length;

            if (this.MqttProtocolVersion == MQTT_PROTOCOL_VERSION.MQTT_3_1_1)
            {
                // [v3.1.1]
                // protocol level
                Array.Copy(MQTT_3_1_1_PROTOCOL_LEVEL, 0, packetData, packetDataIndex, MQTT_3_1_1_PROTOCOL_LEVEL.Length);
                packetDataIndex += MQTT_3_1_1_PROTOCOL_LEVEL.Length;
            }
            else if (this.MqttProtocolVersion == MQTT_PROTOCOL_VERSION.MQTT_5_0)
            {
                // [v5.0]
                // protocol level
                Array.Copy(MQTT_5_0_PROTOCOL_LEVEL, 0, packetData, packetDataIndex, MQTT_5_0_PROTOCOL_LEVEL.Length);
                packetDataIndex += MQTT_5_0_PROTOCOL_LEVEL.Length;
            }

            // connect flags
            if (this.MqttProtocolVersion == MQTT_PROTOCOL_VERSION.MQTT_3_1_1)
            {
                // [v3.1.1]
                packetData[packetDataIndex++] = GetMqtt_3_1_1_Connect_Flags_Byte(this.Username, this.Password, this.WillFlag, this.WillQoSLevel, this.WillRetain, this.CleanSession);

            }
            else if (this.MqttProtocolVersion == MQTT_PROTOCOL_VERSION.MQTT_5_0)
            {
                // [v5.0]
                packetData[packetDataIndex++] = GetMqtt_5_0_Connect_Flags_Byte(this.Username, this.Password, this.WillFlag, this.WillQoSLevel, this.WillRetain, this.CleanSession);
            }

            // add keep alive period
            packetData[packetDataIndex++] = (byte)((this.KeepAlivePeriod >> 8) & 0x00FF); // MSB
            packetData[packetDataIndex++] = (byte)(this.KeepAlivePeriod & 0x00FF); // LSB

            // client identifier
            packetData[packetDataIndex++] = (byte)((this.ClientId.Length >> 8) & 0x00FF); // MSB
            packetData[packetDataIndex++] = (byte)(this.ClientId.Length & 0x00FF); // LSB

            Array.Copy(this.ClientId, 0, packetData, packetDataIndex, this.ClientId.Length);
            packetDataIndex += this.ClientId.Length;

            // will topic
            if (this.WillFlag && (this.WillTopic != null))
            {
                packetData[packetDataIndex++] = (byte)((this.WillTopic.Length >> 8) & 0x00FF); // MSB
                packetData[packetDataIndex++] = (byte)(this.WillTopic.Length & 0x00FF); // LSB
                Array.Copy(this.WillTopic, 0, packetData, packetDataIndex, this.WillTopic.Length);
                packetDataIndex += this.WillTopic.Length;
            }

            // will message
            if (this.WillFlag && (this.WillMessage != null))
            {
                packetData[packetDataIndex++] = (byte)((this.WillMessage.Length >> 8) & 0x00FF); // MSB
                packetData[packetDataIndex++] = (byte)(this.WillMessage.Length & 0x00FF); // LSB
                Array.Copy(this.WillMessage, 0, packetData, packetDataIndex, this.WillMessage.Length);
                packetDataIndex += this.WillMessage.Length;
            }

            // username
            if (this.Username != null)
            {
                packetData[packetDataIndex++] = (byte)((this.Username.Length >> 8) & 0x00FF); // MSB
                packetData[packetDataIndex++] = (byte)(this.Username.Length & 0x00FF); // LSB
                Array.Copy(this.Username, 0, packetData, packetDataIndex, this.Username.Length);
                packetDataIndex += this.Username.Length;
            }

            // password
            if (this.Password != null)
            {
                packetData[packetDataIndex++] = (byte)((this.Password.Length >> 8) & 0x00FF); // MSB
                packetData[packetDataIndex++] = (byte)(this.Password.Length & 0x00FF); // LSB
                Array.Copy(this.Password, 0, packetData, packetDataIndex, this.Password.Length);
                packetDataIndex += this.Password.Length;
            }

            return packetData;
        }

        #region Methods...

        private bool ValidWill(byte[] willTopicBytes, byte[] willMessageBytes, bool willFlag,  QoS_Level willQoSLevel, bool willRetain)
        {
            if (willFlag)
            {
                if (willQoSLevel < QoS_Level.QoS_1 || willQoSLevel > QoS_Level.QoS_2) return false;
                if (willTopicBytes == null) return false;
                if (willTopicBytes != null && willTopicBytes.Length == 0) return false;
                if (willMessageBytes == null) return false;
                if (willMessageBytes != null && willMessageBytes.Length == 0) return false;
            }
            else
            {
                //if (this.WillQoSLevel > QoS_Level.QoS_0) return false;
                if (willRetain) return false;
                if (willTopicBytes != null && willTopicBytes.Length != 0) return false;
                if (willMessageBytes != null && willMessageBytes.Length != 0) return false;
            }

            return true;
        }

        private byte GetMqtt_3_1_1_Connect_Flags_Byte(byte[] usernameBytes, byte[] passwordBytes, bool willFlag, QoS_Level willQoSLevel, bool willRetain, bool cleanSession)
        {
            byte connectFlags = 0x00;
            connectFlags |= (usernameBytes != null) ? (byte)(1 << 7) : (byte)0x00;
            connectFlags |= (passwordBytes != null) ? (byte)(1 << 6) : (byte)0x00;
            connectFlags |= (willRetain) ? (byte)(1 << 5) : (byte)0x00;
            // only if will flag is set, we have to use will QoS level (otherwise is MUST be 0)
            if (willFlag)
            {
                connectFlags |= (byte)((int)willQoSLevel << 3);
            }

            connectFlags |= (willFlag) ? (byte)(1 << 2) : (byte)0x00;
            connectFlags |= (cleanSession) ? (byte)(1 << 1) : (byte)0x00;

            return connectFlags;
        }

        private byte GetMqtt_5_0_Connect_Flags_Byte(byte[] usernameBytes, byte[] passwordBytes, bool willFlag, QoS_Level willQoSLevel, bool willRetain, bool cleanSession)
        {
            byte connectFlags = 0x00;
            connectFlags |= (usernameBytes != null) ? (byte)(1 << 7) : (byte)0x00;
            connectFlags |= (passwordBytes != null) ? (byte)(1 << 6) : (byte)0x00;
            connectFlags |= (willRetain) ? (byte)(1 << 5) : (byte)0x00;
            // only if will flag is set, we have to use will QoS level (otherwise is MUST be 0)
            if (willFlag)
            {
                connectFlags |= (byte)((int)willQoSLevel << 3);
            }

            connectFlags |= (willFlag) ? (byte)(1 << 2) : (byte)0x00;
            connectFlags |= (cleanSession) ? (byte)(1 << 1) : (byte)0x00;

            return connectFlags;
        }

        #endregion
    }
}
