using MQSharp.ControlPackets;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Linq;

namespace MQSharp
{
    internal class PublishQueue
    {
        //private List<PublishMessage> _messageQueue = new List<PublishMessage>();

        //public PublishQueue()
        //{

        //}

        //public void Push(PublishMessage publishMessage)
        //{
        //    _messageQueue.Insert(publishMessage.MessageId, publishMessage);
        //}

        //public void Remove(PublishMessage publishMessage)
        //{
        //    if(_messageQueue.Count > 0)
        //    {
        //        _messageQueue.Remove(publishMessage);

        //    }

        //}

        //public void Update(int messageId, PublishMessage publishMessage)
        //{
        //    try
        //    {
        //        if (_messageQueue.Count > 0)
        //        {
        //            _messageQueue[messageId] = publishMessage;

        //        }
        //    }
        //    catch (Exception) { }
        //}

        //public PublishMessage Pop(double messageResendInterval)
        //{
        //    DateTime timeOlderThanResendInterval = DateTime.Now.AddMilliseconds(-messageResendInterval);

        //    if(_messageQueue.Count > 0)
        //    {
        //        try
        //        {
        //            return _messageQueue
        //                .Where(i => i.LastModifiedTime < timeOlderThanResendInterval)
        //                .OrderBy(i => i.LastModifiedTime)
        //                .FirstOrDefault();
        //        }
        //        catch (Exception)
        //        {
        //            return null;
        //        }
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        //public PublishMessage Pop(int messageId)
        //{
        //    if(_messageQueue.Count > 0)
        //    {
        //        try
        //        {
        //            return _messageQueue[messageId];
        //        }
        //        catch (Exception)
        //        {
        //            return null;
        //        }
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}

        //public void Clear()
        //{
        //    _messageQueue.Clear();
        //}


        private PublishMessage[] _messageQueue = new PublishMessage[65535];

        public PublishQueue()
        {

        }

        public void Push(PublishMessage publishMessage)
        {
            _messageQueue[publishMessage.MessageId] = publishMessage;
        }

        public void Remove(int messageId)
        {
            if (_messageQueue.Length > 0)
            {
                _messageQueue[messageId] = new PublishMessage();

            }

        }

        public void Update(int messageId, PublishMessage publishMessage)
        {
            try
            {
                if (_messageQueue.Length > 0)
                {
                    _messageQueue[messageId] = publishMessage;

                }
            }
            catch (Exception) { }
        }

        public PublishMessage Pop(double messageResendInterval)
        {
            DateTime timeOlderThanResendInterval = DateTime.Now.AddMilliseconds(-messageResendInterval);

            if (_messageQueue.Length > 0)
            {
                try
                {
                    return _messageQueue
                        .Where(i => i.LastModifiedTime < timeOlderThanResendInterval)
                        .OrderBy(i => i.LastModifiedTime)
                        .FirstOrDefault();
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public PublishMessage Pop(int messageId)
        {
            if (_messageQueue.Length > 0)
            {
                try
                {
                    return _messageQueue[messageId];
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public void Clear()
        {
            for(int i=0; i< 65535; i++)
            {
                _messageQueue[i] = new PublishMessage();
            }
        }

    }

    internal class PublishMessage
    {
        public int MessageId { get; set; }
        public PUBLISH PublishPacket { get; set; }
        public QoS_Level QoSLevel { get; set; }
        public DateTime LastModifiedTime { get; set; }

        // 1 = PUBLISH with QoS 1 and waiting for PUBACK
        // 2 = PUBLISH with QoS 2 and waiting for PUBREC
        // 3 = PUBREC receive
        // 4 = PUBREL send and waiting for PUBCOMP
        public int Status { get; set; }
    }

    internal class ReceiveMessage
    {

    }

    #region Enums...

    public enum MQTT_PROTOCOL_VERSION
    {
        MQTT_3_1_1 = 0x04,
        MQTT_5_0 = 0x05
    }

    public enum TLS_PROTOCOL_VERSION
    {
        TLS_1_1,
        TLS_1_2
    }

    public enum QoS_Level
    {
        QoS_0 = 0,
        QoS_1 = 1,
        QoS_2 = 2
    }

    #endregion

    #region EventArgs...

    public class ConnectEventArgs : EventArgs
    {
        public byte ReturnCode { get; set; }
        public string ReturnMessage { get; set; }
    }

    public class DisconnectedEventArgs : EventArgs
    {

    }

    public class SubackReturnData
    {
        public byte ReturnCode { get; set; }
        public string ReturnMessage { get; set; }
    }

    public class SubscribedEventArgs : EventArgs
    {
        public int MessageId { get; set; }
        public List<SubackReturnData> ReturnData { get; set; }
    }

    public class UnsubscribedEventArgs : EventArgs
    {
        public int MessageId { get; set; }
    }

    public class PublishedEventArgs : EventArgs
    {
        public int MessageId { get; set; }
        public int QoSLevel { get; set; }
    }

    public class PublishReceivedEventArgs : EventArgs
    {
        public string Topic { get; set; }
        public int QoSLevel { get; set; }
        public string Payload { get; set; }
    }

    #endregion
}
