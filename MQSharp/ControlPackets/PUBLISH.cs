using MQSharp.Networking;
using System;
using System.Collections.Generic;
using System.Text;

namespace MQSharp.ControlPackets
{
    internal class PUBLISH : ControlPacket
    {
        private string _topic;
        private QoS_Level _qosLevel;
        private byte[] _payload;
        private bool _dupFlag;
        private bool _retain;

        public int PacketId { get; set; }
        public string Topic { get; set; }
        public QoS_Level QoSLevel { get; set; }
        public bool Duplicate 
        {
            get
            {
                return _dupFlag;
            }
            set
            {
                _dupFlag = value;
            }
        }
        public bool Retain { get; set; }
        public string Payload { get; set; }

        public PUBLISH() 
        { 
        
        }

        public PUBLISH(string topic, string payload, bool dupFlag, QoS_Level qosLevel, bool retain)
        {
            this._topic = topic;
            this._payload = Encoding.UTF8.GetBytes(payload);
            this._dupFlag = dupFlag;
            this._qosLevel = qosLevel;
            this._retain = retain;
        }

        public byte[] GetPacketData(MQTT_PROTOCOL_VERSION protocolVersion)
        {
            int fixedHeaderSize = 0;
            int variableHeaderSize = 0;
            int payloadSize = 0;
            int remainingLength = 0;
            byte[] packetData;
            int packetDataIndex = 0;

            // topic can't contain wildcards
            if ((this._topic.IndexOf('#') != -1) || (this._topic.IndexOf('+') != -1))
                throw new Exception("Topic cannot contain # or +");

            // check topic length
            if ((this._topic.Length < Settings.MIN_TOPIC_LENGTH) || (this._topic.Length > Settings.MAX_TOPIC_LENGTH))
                throw new Exception("Invalid topic length");

            // check wrong QoS level (both bits can't be set 1)
            if (this._qosLevel > QoS_Level.QoS_2)
                throw new Exception("Qrong QoS");

            byte[] topicBytes = Encoding.UTF8.GetBytes(this._topic);

            // topic name
            variableHeaderSize += topicBytes.Length + 2;

            // message id is valid only with QOS level 1 or QOS level 2
            if ((this._qosLevel == QoS_Level.QoS_1) || (this._qosLevel == QoS_Level.QoS_2))
            {
                variableHeaderSize += 2; // message id 2 bytes
            }

            // check on message with zero length
            if (this._payload != null)
            {
                // message data
                payloadSize += this._payload.Length;
            } 

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
            packetData[packetDataIndex] = (byte)((CONTROL_PACKET_TYPE_PUBLISH_BYTE << 4));
            packetData[packetDataIndex] |= this._dupFlag ? (byte)(0x01 << 3) : (byte)0x00;
            packetData[packetDataIndex] |= (byte)((int)this._qosLevel << 1);
            packetData[packetDataIndex] |= this._retain ? (byte)0x01 : (byte)0x00;
            packetDataIndex++;

            // encode remaining length
            packetDataIndex = this.EncodeRemainingLength(remainingLength, packetData, packetDataIndex);

            // topic name
            packetData[packetDataIndex++] = (byte)((topicBytes.Length >> 8) & 0x00FF); // MSB
            packetData[packetDataIndex++] = (byte)(topicBytes.Length & 0x00FF); // LSB
            Array.Copy(topicBytes, 0, packetData, packetDataIndex, topicBytes.Length);
            packetDataIndex += topicBytes.Length;

            // message id is valid only with QOS level 1 or QOS level 2
            if ((this._qosLevel == QoS_Level.QoS_1) ||(this._qosLevel == QoS_Level.QoS_2))
            {
                // check message identifier assigned
                if (this.PacketId == 0)
                    throw new Exception("Wrong packet id");

                packetData[packetDataIndex++] = (byte)((this.PacketId >> 8) & 0x00FF); // MSB
                packetData[packetDataIndex++] = (byte)(this.PacketId & 0x00FF); // LSB
            }

            // check on message with zero length
            if (this._payload != null)
            {
                // message data
                Array.Copy(this._payload, 0, packetData, packetDataIndex, this._payload.Length);
                packetDataIndex += this._payload.Length;
            }

            return packetData;
        }

        public static PUBLISH Decode(byte fixedHeaderFirstByte, MQTT_PROTOCOL_VERSION protocolVersion, NetworkTunnel networkTunnel)
        {
            byte[] packetData;
            int packetDataIndex = 0;
            byte[] topicBytes;
            int topicBytesLength;
            PUBLISH publish = new PUBLISH();

            // get remaining length and allocate buffer
            int remainingLength = ControlPacket.DecodeRemainingLength(networkTunnel);
            packetData = new byte[remainingLength];

            // read bytes from socket...
            int received = networkTunnel.ReceiveStream(packetData);

            // topic name
            topicBytesLength = ((packetData[packetDataIndex++] << 8) & 0xFF00);
            topicBytesLength |= packetData[packetDataIndex++];
            topicBytes = new byte[topicBytesLength];
            Array.Copy(packetData, packetDataIndex, topicBytes, 0, topicBytesLength);
            packetDataIndex += topicBytesLength;
            publish.Topic = new String(Encoding.UTF8.GetChars(topicBytes));

            // read DUP flag from fixed header
            publish.Duplicate = ((fixedHeaderFirstByte & 8) >> 3) == 0x01;

            // read QoS level from fixed header
            int qosLevel = (int)((fixedHeaderFirstByte & 6) >> 1);
            if(qosLevel == 0)
            {
                publish.QoSLevel = QoS_Level.QoS_0;
            }
            else if(qosLevel == 1)
            {
                publish.QoSLevel = QoS_Level.QoS_1;
            }
            else if (qosLevel == 2)
            {
                publish.QoSLevel = QoS_Level.QoS_2;
            }
            else
            {
                throw new Exception("Wrong QoS level");
            }

            // read retain flag from fixed header
            publish.Retain = (fixedHeaderFirstByte & 1) == 0x01;

            // message id is valid only with QOS level 1 or QOS level 2
            if ((publish.QoSLevel == QoS_Level.QoS_1) || (publish.QoSLevel == QoS_Level.QoS_2))
            {
                // message id
                publish.PacketId = (int)((packetData[packetDataIndex++] << 8) & 0xFF00);
                publish.PacketId |= (packetData[packetDataIndex++]);
            }

            // get payload with message data
            int messageSize = remainingLength - packetDataIndex;
            int remaining = messageSize;
            int messageIndex = 0;
            byte[] message = new byte[messageSize];

            // payload could be long which have to read part by part

            // copy first part of payload data received
            Array.Copy(packetData, packetDataIndex, message, messageIndex, received - packetDataIndex);
            remaining -= (received - packetDataIndex);
            messageIndex += (received - packetDataIndex);

            // if payload isn't finished
            while (remaining > 0)
            {
                // receive other payload data
                received = networkTunnel.ReceiveStream(packetData);
                Array.Copy(packetData, 0, message, messageIndex, received);
                remaining -= received;
                messageIndex += received;
            }

            publish.Payload = new String(Encoding.UTF8.GetChars(message));

            return publish;
        }
    }
}
