using MQSharp;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace SampleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            X509Certificate2 clientCert = new X509Certificate2("dms-commandprocessor-cert.p12", "root");
            var client = new MqttClient("127.0.0.1", 1883, "sample-client", "sample-client", "P@ssw0rd1", true, 60, MQTT_PROTOCOL_VERSION.MQTT_3_1_1);
            client.MessageResendInterval = 30000;

            client.OnConnected += OnConnect;
            client.OnPingSent += OnPing;
            client.OnPingResponseReceived += OnPingResponse;
            client.OnSubscribed += OnSubscribed;
            client.OnUnsubscribed += OnUnsubscribed;
            client.OnPublished += OnPublished;
            client.OnPublishReceived += OnPublishReceived;

            var connected = client.Connect();

            if (connected)
            {
                var subscriptionId = client.Subscribe(new string[] { "works/subscribe", "works/subscribe2", "works/subscribe3" }, new QoS_Level[] { QoS_Level.QoS_1, QoS_Level.QoS_0, QoS_Level.QoS_2 });
                Console.WriteLine($"Subscription requested for: {subscriptionId}");

                var unsubscribeId = client.Unsubscribe(new string[] { "works/subscribe2" }, new QoS_Level[] { QoS_Level.QoS_0 });
                Console.WriteLine($"Unsubscribed requested for: {unsubscribeId}");

                //Thread.Sleep(20000);

                var qos = QoS_Level.QoS_0;
                client.Publish("this is the first publish message", "works/publish", qos);
                qos = QoS_Level.QoS_1;
                client.Publish("this is the second publish message", "works/publish", qos);

                //Thread.Sleep(20000);

                qos = QoS_Level.QoS_2;
                client.Publish("this is the third publish message", "works/publish", qos);

                Thread.Sleep(5000);

                //var unsubscriptionId = client.Unsubscribe(new string[] { "works/topics" }, new QoS_Level[] { QoS_Level.QoS_1 });
                //Console.WriteLine($"Unubscribed requested for: {unsubscriptionId}");
            }


            Console.ReadLine();
        }

        static void OnConnect(object sender, ConnectEventArgs e)
        {
            Console.WriteLine($"Return Code: {e.ReturnCode}, Return Message: {e.ReturnMessage}");
        }

        static void OnPing(object sender, EventArgs e)
        {
            Console.WriteLine($"Ping to broker at {DateTime.Now}");
        }

        static void OnPingResponse(object sender, EventArgs e)
        {
            Console.WriteLine($"Ping response from the broker at {DateTime.Now}");
        }

        static void OnSubscribed(object sender, SubscribedEventArgs e)
        {
            Console.WriteLine($"Subscribed for Message Id: {e.MessageId}");
            Console.WriteLine("Granted QoS:");
            for(int i=0; i< e.ReturnData.Count; i++)
            {
                Console.WriteLine($"{i}. {e.ReturnData[i].ReturnCode}, {e.ReturnData[i].ReturnMessage}");
            }
        }

        static void OnUnsubscribed(object sender, UnsubscribedEventArgs e)
        {
            Console.WriteLine($"Unsubscribed for Message Id: {e.MessageId}");
        }

        static void OnPublished(object sender, PublishedEventArgs e)
        {
            Console.WriteLine($"Message published with Message Id: {e.MessageId} and QoS Level: {e.QoSLevel}");
        }

        static void OnPublishReceived(object sender, PublishReceivedEventArgs e)
        {
            Console.WriteLine($"Message received, topic: {e.Topic}, QoS Level: {e.QoSLevel}, Payload: {e.Payload}");
        }
    }
}
