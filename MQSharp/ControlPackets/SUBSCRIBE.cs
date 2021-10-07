using System;
using System.Collections.Generic;
using System.Text;

namespace MQSharp.ControlPackets
{
    internal class SUBSCRIBE : ControlPacket
    {
        private string[] _topics;
        private QoS_Level[] _qosLevels;
        private MQTT_PROTOCOL_VERSION _protocolVersion;

        public int PacketId { get; set; }

        public SUBSCRIBE(string[] topics, QoS_Level[] qosLevels, MQTT_PROTOCOL_VERSION protoclVersion)
        {
            _topics = topics;
            _qosLevels = qosLevels;
            _protocolVersion = protoclVersion;
        }

        public byte[] GetPacketData()
        {
            int fixedHeaderSize = 0;
            int variableHeaderSize = 0;
            int payloadSize = 0;
            int remainingLength = 0;
            byte[] packetData;
            int packetDataIndex = 0;

            // topics list empty
            if ((this._topics == null) || (this._topics.Length == 0))
            {
                throw new Exception("Topics empty");
            }
                

            // qos levels list empty
            if ((this._qosLevels == null) || (this._qosLevels.Length == 0))
            {
                throw new Exception("Qos Level empty");
            }
                

            // topics and qos levels lists length don't match
            if (this._topics.Length != this._qosLevels.Length)
            {
                throw new Exception("Topics and qos count doesn't matched");
            }

            // check message identifier assigned (SUBSCRIBE uses QoS Level 1, so message id is mandatory)
            if (this.PacketId == 0)
            {
                throw new Exception("Wrong packet Id");
            }

            if(_protocolVersion == MQTT_PROTOCOL_VERSION.MQTT_3_1_1)
            {
                // message identifier
                variableHeaderSize += 2; // message id 2 bytes

                int topicIndex = 0;
                byte[][] topicBytes = new byte[this._topics.Length][];

                for (topicIndex = 0; topicIndex < this._topics.Length; topicIndex++)
                {
                    // check topic length
                    if ((this._topics[topicIndex].Length < Settings.MIN_TOPIC_LENGTH) || (this._topics[topicIndex].Length > Settings.MAX_TOPIC_LENGTH))
                    {
                        throw new Exception("Topics list not valid");
                    }

                    topicBytes[topicIndex] = Encoding.UTF8.GetBytes(this._topics[topicIndex]);
                    payloadSize += 2; // topic size (MSB, LSB)
                    payloadSize += topicBytes[topicIndex].Length;
                    payloadSize++; // byte for QoS
                }

                remainingLength += (variableHeaderSize + payloadSize);

                // first byte of fixed header
                fixedHeaderSize += CONTROL_PACKET_TYPE_SUBSCRIBE.Length;

                // increase fixed header size based on remaining length
                // (each remaining length byte can encode until 128)
                int temp = remainingLength;

                do
                {
                    fixedHeaderSize++;
                    temp = temp / 128;
                } while (temp > 0);

                // allocate buffer for message
                packetData = new byte[fixedHeaderSize + variableHeaderSize + payloadSize];

                // first fixed header byte
                Array.Copy(CONTROL_PACKET_TYPE_SUBSCRIBE, 0, packetData, packetDataIndex, CONTROL_PACKET_TYPE_SUBSCRIBE.Length);
                packetDataIndex += CONTROL_PACKET_TYPE_SUBSCRIBE.Length;

                // encode remaining length
                packetDataIndex = this.EncodeRemainingLength(remainingLength, packetData, packetDataIndex);

                packetData[packetDataIndex++] = (byte)((this.PacketId >> 8) & 0x00FF); // MSB
                packetData[packetDataIndex++] = (byte)(this.PacketId & 0x00FF); // LSB 

                topicIndex = 0;
                for (topicIndex = 0; topicIndex < _topics.Length; topicIndex++)
                {
                    // topic name
                    packetData[packetDataIndex++] = (byte)((topicBytes[topicIndex].Length >> 8) & 0x00FF); // MSB
                    packetData[packetDataIndex++] = (byte)(topicBytes[topicIndex].Length & 0x00FF); // LSB
                    Array.Copy(topicBytes[topicIndex], 0, packetData, packetDataIndex, topicBytes[topicIndex].Length);
                    packetDataIndex += topicBytes[topicIndex].Length;

                    // requested QoS
                    packetData[packetDataIndex++] = Convert.ToByte(_qosLevels[topicIndex]);
                }

                return packetData;
            }
            else if(_protocolVersion == MQTT_PROTOCOL_VERSION.MQTT_5_0)
            {
                
            }

            return new byte[] { };
        }
    }
}
