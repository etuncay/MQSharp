﻿using MQSharp.ControlPackets;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;
using System.Linq;

namespace MQSharp
{
    internal class PublishQueue
    {
        private Dictionary<int, PublishMessage> messageQueue;

        public PublishQueue()
        {
            messageQueue = new Dictionary<int, PublishMessage>();
        }

        public void Push(PublishMessage publishMessage)
        {
            messageQueue.Add(publishMessage.MessageId, publishMessage);
        }

        public bool Remove(int messageId)
        {
            return messageQueue.Remove(messageId);
        }

        public void Update(int messageId, PublishMessage publishMessage)
        {
            messageQueue[messageId] = publishMessage;
        }

        public PublishMessage Pop(double messageResendInterval)
        {
            DateTime timeOlderThanResendInterval = DateTime.Now.AddMilliseconds(-messageResendInterval);

            return messageQueue
                .Values
                .Where(i => i.LastModifiedTime < timeOlderThanResendInterval)
                .OrderBy(i => i.LastModifiedTime)
                .FirstOrDefault();
        }

        public PublishMessage Pop(int messageId)
        {
            return messageQueue.Where(i => i.Key == messageId).Select(i => i.Value).FirstOrDefault();
        }

        public void Clear()
        {
            messageQueue.Clear();
        }

        //private PublishMessage[] messageQueue = new PublishMessage[65535];

        //public PublishQueue()
        //{

        //}

        //public void Push(PublishMessage publishMessage)
        //{
        //    messageQueue[publishMessage.MessageId] = publishMessage;
        //}

        //public void Remove(int messageId)
        //{
        //    if (messageQueue.Length > 0)
        //    {
        //        messageQueue[messageId] = new PublishMessage();

        //    }

        //}

        //public void Update(int messageId, PublishMessage publishMessage)
        //{
        //    try
        //    {
        //        if (messageQueue.Length > 0)
        //        {
        //            messageQueue[messageId] = publishMessage;

        //        }
        //    }
        //    catch (Exception) { }
        //}

        //public PublishMessage Pop(double messageResendInterval)
        //{
        //    DateTime timeOlderThanResendInterval = DateTime.Now.AddMilliseconds(-messageResendInterval);

        //    if (messageQueue.Length > 0)
        //    {
        //        try
        //        {
        //            return messageQueue
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
        //    if (messageQueue.Length > 0)
        //    {
        //        try
        //        {
        //            return messageQueue[messageId];
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
        //    for(int i=0; i< 65535; i++)
        //    {
        //        messageQueue[i] = new PublishMessage();
        //    }
        //}

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
