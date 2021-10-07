// 09 May 2020 Satardy 12:11 PM
// 619 BUKIT PANJANG RING RD #03-812 SINGAPORE 670619
// MOSTAFIZUR RAHMAN

using MQSharp.ControlPackets;
using MQSharp.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace MQSharp
{
    public class MqttClient
    {
        #region Variables...

        private bool running = false;
        private DateTime lastMessageTime;
        private int lastPacketId = 1;
        private PublishQueue publishQueue = new PublishQueue();

        private NetworkTunnel networkTunnel;
        private CONNACK connackResponse = null;
        private PINGRESP pingrespResponse = null;

        // Events
        private AutoResetEvent connectionDoneEvent = new AutoResetEvent(false);
        public event EventHandler<ConnectEventArgs> OnConnected;
        public event EventHandler OnDisconnected;
        public event EventHandler<SubscribedEventArgs> OnSubscribed;
        public event EventHandler<UnsubscribedEventArgs> OnUnsubscribed;
        public event EventHandler<PublishedEventArgs> OnPublished;
        public event EventHandler<PublishReceivedEventArgs> OnPublishReceived;
        public event EventHandler OnPingSent;
        public event EventHandler OnPingResponseReceived;

        #endregion

        #region properties...

        public string BrokerHost { get; set; }
        public int BrokerPort { get; set; }
        public string ClientId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Secure { get; set; }
        public X509Certificate CACert { get; set; }
        public X509Certificate ClientCert { get; set; }
        public bool WillFlag { get; set; } = false;
        public string WillTopic { get; set; } = null;
        public string WillMessage { get; set; } = null;
        public bool WillRetain { get; set; } = false;
        public bool CleanSession { get; set; }
        public int KeepAlivePeriod { get; set; }
        public MQTT_PROTOCOL_VERSION MQTTProtocolVersion { get; set; }
        public TLS_PROTOCOL_VERSION TLSProtocolVersion { get; set; }
        public double MessageResendInterval { get; set; } = 30000; // millisecond, deafult 30 sec
        public bool IsConnected { get; set; }

        #endregion

        #region Constructor...

        public MqttClient()
        {
            // localhost 127.0.0.1
            // none ssl port 1883
            // anonymouse
            // random clientId
            // random username
            // null password
            // not secure
            // clean session
            // 60 sec keep alive period
            // MQTT protocol version 3.1.1
            // TLS protocl version 1.2
            // Message resend interval 30 sec

            string guid = Guid.NewGuid().ToString();
            this.Initialize("127.0.0.1", 1883, guid, guid, null, null, null, true, 60, MQTT_PROTOCOL_VERSION.MQTT_3_1_1, TLS_PROTOCOL_VERSION.TLS_1_2);
        }

        public MqttClient(string brokerHost, int brokerPort, string clientId, string username, string password, bool cleanSession, int keepAlivePeriod, MQTT_PROTOCOL_VERSION mqttProtocolVersion)
        {
            this.Initialize(brokerHost, brokerPort, clientId, username, password, null, null, cleanSession, keepAlivePeriod, mqttProtocolVersion, TLS_PROTOCOL_VERSION.TLS_1_2);
        }

        public MqttClient(string brokerHost, int brokerPort, string clientId, string username, string password, X509Certificate caCert, X509Certificate clientCert, bool cleanSession, int keepAlivePeriod, TLS_PROTOCOL_VERSION tlsProtocolVersion, MQTT_PROTOCOL_VERSION mqttProtocolVersion)
        {
            this.Initialize(brokerHost, brokerPort, clientId, username, password, caCert, clientCert, cleanSession, keepAlivePeriod, mqttProtocolVersion, tlsProtocolVersion);
        }

        #endregion

        #region Public Methods...

        public bool Connect()
        {
            networkTunnel = new NetworkTunnel(this.BrokerHost, this.BrokerPort, this.Secure, this.CACert, this.ClientCert, this.TLSProtocolVersion);

            bool tunnelOpen = networkTunnel.Open();

            if (tunnelOpen)
            {
                this.running = true;

                ThreadManager.StartThread(this.ReceivingEngine);

                CONNECT connect = new CONNECT(this.ClientId, this.Username, this.Password, this.WillFlag, this.WillTopic, this.WillMessage, this.WillRetain, QoS_Level.QoS_0, this.CleanSession, this.KeepAlivePeriod, this.MQTTProtocolVersion);

                var connectPacketData = connect.GetPacketData();

                networkTunnel.SendStream(connectPacketData);

                lastMessageTime = DateTime.Now;

                connectionDoneEvent.WaitOne(Settings.DEFAULT_TIMEOUT);

                if (connackResponse.Accepted == true)
                {
                    ThreadManager.StartThread(this.KeepAliveEngine);
                    ThreadManager.StartThread(this.PublishEngine);

                    this.IsConnected = true;

                    if(this.CleanSession == true || connackResponse.SessionPresent == false)
                    {
                        // Clean all session data
                        lastPacketId = 1;
                        publishQueue.Clear();
                    }

                    return true;
                }
                else if (connackResponse.Accepted == false)
                {
                    this.IsConnected = false;

                    return false;
                }
                else
                {
                    throw new TimeoutException();
                }
                
            }

            return false;

        }

        public void Disconnect()
        {
            DISCONNECT disconnect = new DISCONNECT(this.MQTTProtocolVersion);

            var disconnectPacketData = disconnect.GetPacketData();

            networkTunnel.SendStream(disconnectPacketData);

            this.running = false;
        }

        public int Subscribe(string[] topics, QoS_Level[] qosLevels)
        {
            SUBSCRIBE subscribe = new SUBSCRIBE(topics, qosLevels, this.MQTTProtocolVersion);
            subscribe.PacketId = GetUniquePacketId();

            var subscribePacketData = subscribe.GetPacketData();

            networkTunnel.SendStream(subscribePacketData);

            lastMessageTime = DateTime.Now;

            return subscribe.PacketId;
        }

        public int Unsubscribe(string[] topics, QoS_Level[] qosLevels)
        {
            UNSUBSCRIBE unsubscribe = new UNSUBSCRIBE(topics, qosLevels, this.MQTTProtocolVersion);
            unsubscribe.PacketId = GetUniquePacketId();

            var unsubscribePacketData = unsubscribe.GetPacketData();

            networkTunnel.SendStream(unsubscribePacketData);

            lastMessageTime = DateTime.Now;

            return unsubscribe.PacketId;
        }

        public void Publish(string message, string topic, QoS_Level qosLevel)
        {
            PUBLISH publish = new PUBLISH(topic, message, false, qosLevel, false);

            PublishMessage publishMessage = new PublishMessage();

            if (qosLevel == QoS_Level.QoS_1)
            {
                publish.PacketId = GetUniquePacketId();

                publishMessage.MessageId = publish.PacketId;
                publishMessage.PublishPacket = publish;
                publishMessage.QoSLevel = qosLevel;
                publishMessage.Status = 1;
                publishMessage.LastModifiedTime = DateTime.Now;

                publishQueue.Push(publishMessage);

                var publishPacketData = publish.GetPacketData(this.MQTTProtocolVersion);
                networkTunnel.SendStream(publishPacketData);
            }
            else if(qosLevel == QoS_Level.QoS_2)
            {
                publish.PacketId = GetUniquePacketId();

                publishMessage.MessageId = publish.PacketId;
                publishMessage.PublishPacket = publish;
                publishMessage.QoSLevel = qosLevel;
                publishMessage.Status = 2;
                publishMessage.LastModifiedTime = DateTime.Now;

                publishQueue.Push(publishMessage);

                var publishPacketData = publish.GetPacketData(this.MQTTProtocolVersion);
                networkTunnel.SendStream(publishPacketData);
            }
            else if(qosLevel == QoS_Level.QoS_0)
            {
                publish.PacketId = 0;

                var publishPacketData = publish.GetPacketData(this.MQTTProtocolVersion);
                networkTunnel.SendStream(publishPacketData);

                PublishedEventArgs eventArgs = new PublishedEventArgs();

                eventArgs.MessageId = publish.PacketId;
                eventArgs.QoSLevel = 0;

                RaiseOnPublishedEvent(eventArgs);
            }

            

            lastMessageTime = DateTime.Now;

            //_publishQueueEmptyEvent.Set();
        }

        #endregion

        #region Events...

        protected virtual void RaiseOnConnectedEvent(ConnectEventArgs e)
        {
            EventHandler<ConnectEventArgs> handler = OnConnected;
            handler?.Invoke(this, e);
        }

        protected virtual void RaiseOnDisconnectedEvent(EventArgs e)
        {
            EventHandler handler = OnDisconnected;
            handler?.Invoke(this, e);
        }

        protected virtual void RaiseOnSubscribedEvent(SubscribedEventArgs e)
        {
            EventHandler<SubscribedEventArgs> handler = OnSubscribed;
            handler?.Invoke(this, e);
        }

        protected virtual void RaiseOnUnsubscribedEvent(UnsubscribedEventArgs e)
        {
            EventHandler<UnsubscribedEventArgs> handler = OnUnsubscribed;
            handler?.Invoke(this, e);
        }

        protected virtual void RaiseOnPublishedEvent(PublishedEventArgs e)
        {
            EventHandler<PublishedEventArgs> handler = OnPublished;
            handler?.Invoke(this, e);
        }

        protected virtual void RaiseOnPublishReceivedEvent(PublishReceivedEventArgs e)
        {
            EventHandler<PublishReceivedEventArgs> handler = OnPublishReceived;
            handler?.Invoke(this, e);
        }

        protected virtual void RaiseOnPingSentEvent(EventArgs e)
        {
            EventHandler handler = OnPingSent;
            handler?.Invoke(this, e);
        }

        protected virtual void RaiseOnPingResponseReceivedEvent(EventArgs e)
        {
            EventHandler handler = OnPingResponseReceived;
            handler?.Invoke(this, e);
        }

        #endregion

        #region Private Methods...

        private void Initialize(
            string brokerHost, 
            int brokerPort, 
            string clientId, 
            string username, 
            string password, 
            //bool secure, 
            X509Certificate caCert,
            X509Certificate clientCert, 
            bool cleanSession, 
            int keepAlivePeriod, 
            MQTT_PROTOCOL_VERSION mqttProtocolVersion, 
            TLS_PROTOCOL_VERSION tlsProtocolVersion
            )
        {
            this.BrokerHost = brokerHost;
            this.BrokerPort = brokerPort;
            this.ClientId = clientId;
            this.Username = username;
            this.Password = password;
            if (caCert != null || clientCert != null)
            {
                this.Secure = true;
            }
            else
            {
                this.Secure = false;
            }
            
            this.CACert = caCert;
            this.ClientCert = clientCert;
            this.WillFlag = false;
            this.WillTopic = string.Empty;
            this.WillMessage = string.Empty;
            this.WillRetain = false;
            this.CleanSession = cleanSession;
            this.KeepAlivePeriod = keepAlivePeriod;
            this.MQTTProtocolVersion = mqttProtocolVersion;
            this.TLSProtocolVersion = tlsProtocolVersion;
            this.MessageResendInterval = 30000;
        }

        private void ReceivingEngine()
        {
            byte[] fixedHeaderFirstByte = new byte[1];

            while (this.running)
            {
                try
                {
                    // read first byte (fixed header)
                    this.networkTunnel.ReceiveStream(fixedHeaderFirstByte);

                    if (fixedHeaderFirstByte[0] > 0x00)
                    {
                        switch (fixedHeaderFirstByte[0])
                        {
                            // CONNACK received from server
                            case ControlPacket.CONTROL_PACKET_TYPE_CONNACK_BYTE:
                                {
                                    connackResponse = CONNACK.Decode(this.MQTTProtocolVersion, networkTunnel);
                                    ProcessCONNACK(connackResponse);
                                }
                                break;
                            case ControlPacket.CONTROL_PACKET_TYPE_PINGRESP_BYTE:
                                {
                                    pingrespResponse = PINGRESP.Decode(this.MQTTProtocolVersion, networkTunnel);
                                    ProcessPINGRESP(pingrespResponse);
                                }
                                break;
                            case ControlPacket.CONTROL_PACKET_TYPE_SUBACK_BYTE:
                                {
                                    var subackResponse = SUBACK.Decode(this.MQTTProtocolVersion, networkTunnel);
                                    ProcessSUBACK(subackResponse);
    }
                                break;
                            case ControlPacket.CONTROL_PACKET_TYPE_UNSUBACK_BYTE:
                                {
                                    var unsubackResponse = UNSUBACK.Decode(this.MQTTProtocolVersion, networkTunnel);
                                    ProcessUNSUBACK(unsubackResponse);
                                }
                                break;
                            case ControlPacket.CONTROL_PACKET_TYPE_PUBACK_BYTE:
                                {
                                    var pubackResponse = PUBACK.Decode(fixedHeaderFirstByte[0], this.MQTTProtocolVersion, networkTunnel);
                                    ProcessPUBACK(pubackResponse);
                                }
                                break;
                            case ControlPacket.CONTROL_PACKET_TYPE_PUBREC_BYTE:
                                {
                                    var pubrecResponse = PUBREC.Decode(fixedHeaderFirstByte[0], this.MQTTProtocolVersion, networkTunnel);
                                    ProcessPUBREC(pubrecResponse);
                                }
                                break;
                            case ControlPacket.CONTROL_PACKET_TYPE_PUBREL_BYTE:
                                {
                                    var pubrelResponse = PUBREL.Decode(fixedHeaderFirstByte[0], this.MQTTProtocolVersion, networkTunnel);
                                    ProcessPUBREL(pubrelResponse);
                                }
                                break;
                            case ControlPacket.CONTROL_PACKET_TYPE_PUBCOMP_BYTE:
                                {
                                    var pubcompResponse = PUBCOMP.Decode(fixedHeaderFirstByte[0], this.MQTTProtocolVersion, networkTunnel);
                                    ProcessPUBCOMP(pubcompResponse);
                                }
                                break;
                            default:
                                {
                                    if((byte)((fixedHeaderFirstByte[0] & 0x30) >> 4) == ControlPacket.CONTROL_PACKET_TYPE_PUBLISH_BYTE)
                                    {
                                        var publishPacket = PUBLISH.Decode(fixedHeaderFirstByte[0], this.MQTTProtocolVersion, networkTunnel);
                                        ProcessPUBLISH(publishPacket);
                                    }
                                }
                                break;
                        }
                    }


                }
                catch(Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
        }

        private void PublishEngine()
        {
            while (running)
            {
                var publishMessage = publishQueue.Pop(this.MessageResendInterval);
                if(publishMessage == null)
                {
                    // message queue empty, wait for new publish message
                    //_publishQueueEmptyEvent.WaitOne();

                    Thread.Sleep((int)this.MessageResendInterval);
                    continue;
                }

                switch (publishMessage.Status)
                {
                    case 1: // PUBLISH with QoS 1, waiting for PUBACK
                        {
                            ProcessWaitForPUBACKPublishMessage(publishMessage);
                        }
                        break;
                    case 2: // PUBLISH with QoS 2, waiting for PUBREC
                        {
                            ProcessWaitForPUBRECPublishMessage(publishMessage);
                        }
                        break;
                    case 3: // PUBREC receive
                        {
                            ProcessPUBRECReceivedPublishMessage(publishMessage);
                        }
                        break;

                }

            }
        }

        private void ProcessWaitForPUBACKPublishMessage(PublishMessage publishMessage)
        {
            var publishPacket = publishMessage.PublishPacket;
            publishPacket.Duplicate = true;
            var publishPacketData = publishPacket.GetPacketData(this.MQTTProtocolVersion);

            networkTunnel.SendStream(publishPacketData);

            lastMessageTime = DateTime.Now;

            publishMessage.PublishPacket = publishPacket;
            publishMessage.LastModifiedTime = DateTime.Now;
            publishQueue.Update(publishMessage.MessageId, publishMessage);
        }

        private void ProcessWaitForPUBRECPublishMessage(PublishMessage publishMessage)
        {
            var publishPacket = publishMessage.PublishPacket;
            publishPacket.Duplicate = true;
            var publishPacketData = publishPacket.GetPacketData(this.MQTTProtocolVersion);

            networkTunnel.SendStream(publishPacketData);

            lastMessageTime = DateTime.Now;

            publishMessage.PublishPacket = publishPacket;
            publishMessage.LastModifiedTime = DateTime.Now;
            publishQueue.Update(publishMessage.MessageId, publishMessage);
        }

        private void ProcessPUBRECReceivedPublishMessage(PublishMessage publishMessage)
        {
            PUBREL pubrel = new PUBREL();
            pubrel.PacketId = publishMessage.PublishPacket.PacketId;

            var pubrelPacketData = pubrel.GetPacketData(this.MQTTProtocolVersion);

            networkTunnel.SendStream(pubrelPacketData);

            lastMessageTime = DateTime.Now;

            publishMessage.Status = 4;
            publishQueue.Update(publishMessage.MessageId, publishMessage);
        }

        private void ProcessCONNACK(CONNACK connackPacket)
        {
            string returnMessage;
            Settings.MQTT_3_1_1_CONNACK_RETURN_CODE.TryGetValue(connackPacket.ReturnCode, out returnMessage);

            ConnectEventArgs eventArgs = new ConnectEventArgs();
            eventArgs.ReturnCode = connackPacket.ReturnCode;
            eventArgs.ReturnMessage = returnMessage;

            RaiseOnConnectedEvent(eventArgs);

            connectionDoneEvent.Set();
        }

        private void ProcessPINGRESP(PINGRESP pingrespPacket)
        {
            RaiseOnPingResponseReceivedEvent(EventArgs.Empty);
        }

        private void ProcessSUBACK(SUBACK subackPacket)
        {
            SubscribedEventArgs eventArgs = new SubscribedEventArgs();

            eventArgs.MessageId = subackPacket.PacketId;
            eventArgs.ReturnData = new List<SubackReturnData>();

            foreach(var returnCode in subackPacket.ReturnCodes)
            {
                string returnMessage;
                Settings.MQTT_3_1_1_SUBACK_RETURN_CODE.TryGetValue(returnCode, out returnMessage);

                eventArgs.ReturnData.Add(
                        new SubackReturnData
                        {
                            ReturnCode = returnCode,
                            ReturnMessage = returnMessage
                        }
                    );
            }

            RaiseOnSubscribedEvent(eventArgs);

        }

        private void ProcessUNSUBACK(UNSUBACK unsubackPacket)
        {
            UnsubscribedEventArgs eventArgs = new UnsubscribedEventArgs();

            eventArgs.MessageId = unsubackPacket.PacketId;

            RaiseOnUnsubscribedEvent(eventArgs);
        }

        private void ProcessPUBACK(PUBACK pubackPacket)
        {
            publishQueue.Remove(pubackPacket.PacketId);

            PublishedEventArgs eventArgs = new PublishedEventArgs();

            eventArgs.MessageId = pubackPacket.PacketId;
            eventArgs.QoSLevel = 1;

            RaiseOnPublishedEvent(eventArgs);
        }

        private void ProcessPUBREC(PUBREC pubrecPacket)
        {
            PublishMessage message = publishQueue.Pop(pubrecPacket.PacketId);

            if(message != null)
            {
                message.Status = 3;
                message.LastModifiedTime = DateTime.Now;
                publishQueue.Update(pubrecPacket.PacketId, message);

                PUBREL pubrelPacket = new PUBREL();
                pubrelPacket.PacketId = pubrecPacket.PacketId;
                var pubrelPacketData = pubrelPacket.GetPacketData(this.MQTTProtocolVersion);

                networkTunnel.SendStream(pubrelPacketData);

                lastMessageTime = DateTime.Now;

                message = publishQueue.Pop(pubrecPacket.PacketId);
                message.Status = 4;
                message.LastModifiedTime = DateTime.Now;
                publishQueue.Update(pubrecPacket.PacketId, message);
            }
        }

        private void ProcessPUBREL(PUBREL pubrelPacket)
        {
            PUBCOMP pubcompPacket = new PUBCOMP();
            pubcompPacket.PacketId = pubrelPacket.PacketId;
            var pubcompPacketData = pubcompPacket.GetPacketData(this.MQTTProtocolVersion);

            networkTunnel.SendStream(pubcompPacketData);

            lastMessageTime = DateTime.Now;
        }

        private void ProcessPUBCOMP(PUBCOMP pubcompPacket)
        {
            publishQueue.Remove(pubcompPacket.PacketId);

            PublishedEventArgs eventArgs = new PublishedEventArgs();

            eventArgs.MessageId = pubcompPacket.PacketId;
            eventArgs.QoSLevel = 2;

            RaiseOnPublishedEvent(eventArgs);
        }

        private void ProcessPUBLISH(PUBLISH publishPacket)
        {
            PublishReceivedEventArgs eventArgs = new PublishReceivedEventArgs();

            eventArgs.Topic = publishPacket.Topic;
            eventArgs.QoSLevel = (int)publishPacket.QoSLevel;
            eventArgs.Payload = publishPacket.Payload;

            RaiseOnPublishReceivedEvent(eventArgs);

            if(publishPacket.QoSLevel == QoS_Level.QoS_1)
            {
                PUBACK pubackPacket = new PUBACK();
                pubackPacket.PacketId = publishPacket.PacketId;
                var pubackPacketData = pubackPacket.GetPacketData(this.MQTTProtocolVersion);

                networkTunnel.SendStream(pubackPacketData);

                lastMessageTime = DateTime.Now;
            }
            else if (publishPacket.QoSLevel == QoS_Level.QoS_2)
            {
                PUBREC pubrecPacket = new PUBREC();
                pubrecPacket.PacketId = publishPacket.PacketId;
                var pubrecPacketData = pubrecPacket.GetPacketData(this.MQTTProtocolVersion);

                networkTunnel.SendStream(pubrecPacketData);

                lastMessageTime = DateTime.Now;
            }

        }

        private void KeepAliveEngine()
        {
            double keepAlivePeriodInMillisecond = this.KeepAlivePeriod * 1000;

            while (running)
            {
                double lastMessageSince = GetLastMessageTimeDifferenceInMilliseconds();

                if (lastMessageSince >= keepAlivePeriodInMillisecond)
                {
                    Ping();

                    Thread.Sleep(this.KeepAlivePeriod * 1000);

                    if(pingrespResponse == null)
                    {
                        throw new Exception($"No response from the broker: {this.BrokerHost}");
                    }
                }
                else
                {
                    Thread.Sleep((int)(keepAlivePeriodInMillisecond - lastMessageSince));
                }
            }
        }

        private void Ping()
        {
            PINGREQ pingreqPacket = new PINGREQ();

            networkTunnel.SendStream(pingreqPacket.GetPacketData());

            lastMessageTime = DateTime.Now;

            RaiseOnPingSentEvent(EventArgs.Empty);
        }

        private double GetLastMessageTimeDifferenceInMilliseconds()
        {
            TimeSpan difference = (DateTime.Now - lastMessageTime);
            return difference.TotalMilliseconds;
        }

        private int GetUniquePacketId()
        {
            if(lastPacketId < 65535)
            {
                return lastPacketId++;
            }
            else
            {
                // restart numbering
                lastPacketId = 1;
                return lastPacketId++;
            }
        }

        #endregion

    }
}
