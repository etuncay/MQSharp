using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace MQSharp.Networking
{
    internal class NetworkTunnel
    {
        #region Variables & Objects...

        private Socket socket;
        private SslStream sslStream;

        #endregion

        #region Properties...

        public string RemoteHost { get; set; }
        public int RemotePort { get; set; }
        public bool Secure { get; set; }
        public X509Certificate CACert { get; set; }
        public X509Certificate ClientCert { get; set; }
        public TLS_PROTOCOL_VERSION TLSProtocolVersion { get; set; }

        #endregion

        #region Constructors..

        public NetworkTunnel(string remoteHost, int remotePort, bool secure, X509Certificate caCert, X509Certificate clientCert, TLS_PROTOCOL_VERSION tlsProtocolVersion)
        {
            this.RemoteHost = remoteHost;
            this.RemotePort = remotePort;
            this.Secure = secure;
            this.CACert = caCert;
            this.ClientCert = clientCert;
            this.TLSProtocolVersion = TLSProtocolVersion;
        }

        #endregion

        #region Public Methods...

        public bool Open()
        {
            IPAddress remoteIPAddress = GetIPAddressFromHostName(this.RemoteHost);
            IPEndPoint remoteEndPoint = new IPEndPoint(remoteIPAddress, this.RemotePort);

            // Create a TCP/IP  socket.  
            socket = new Socket(remoteIPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.Connect(remoteEndPoint);

                if (this.Secure)
                {
                    // create SSL stream
                    var netStream = new NetworkStream(socket);
                    sslStream = new SslStream(netStream, false, (a, b, c, d) => { return true; });

                    SslProtocols sslProtocl = SslProtocols.Tls12;
                    if (this.TLSProtocolVersion == TLS_PROTOCOL_VERSION.TLS_1_1)
                    {
                        sslProtocl = SslProtocols.Tls11;
                    }
                    else if(this.TLSProtocolVersion == TLS_PROTOCOL_VERSION.TLS_1_2)
                    {
                        sslProtocl = SslProtocols.Tls12;
                    }

                    // server authentication (SSL/TLS handshake)
                    X509CertificateCollection clientCertificates = new X509CertificateCollection(new X509Certificate[] { this.ClientCert });
                    sslStream.AuthenticateAsClient(this.RemoteHost, clientCertificates, sslProtocl, false);

                }

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void SendStream(byte[] dataBytes)
        {
            if (this.Secure)
            {
                this.sslStream.Write(dataBytes, 0, dataBytes.Length);
                this.sslStream.Flush();
            }
            else
            {
                socket.Send(dataBytes, 0, dataBytes.Length, SocketFlags.None);
            }
            
        }

        public int ReceiveStream(byte[] receivedData)
        {
            if (this.Secure)
            {
                // read all data needed (until fill buffer)
                int idx = 0, read = 0;
                while (idx < receivedData.Length)
                {
                    // fixed scenario with socket closed gracefully by peer/broker and
                    // Read return 0. Avoid infinite loop.
                    read = this.sslStream.Read(receivedData, idx, receivedData.Length - idx);
                    if (read == 0)
                        return 0;
                    idx += read;
                }

                return receivedData.Length;
            }
            else
            {
                // read all data needed (until fill buffer)
                int idx = 0, read = 0;
                while (idx < receivedData.Length)
                {
                    // fixed scenario with socket closed gracefully by peer/broker and
                    // Read return 0. Avoid infinite loop.
                    read = this.socket.Receive(receivedData, idx, receivedData.Length - idx, SocketFlags.None);
                    if (read == 0)
                        return 0;
                    idx += read;
                }
                return receivedData.Length;
            }

        }

        public void Close()
        {
            socket.Close();
        }

        #endregion

        #region  Private Methods...

        private IPAddress GetIPAddressFromHostName(string hostName)
        {
            IPAddress ipAddress;
            bool validIPAddress = IPAddress.TryParse(hostName, out ipAddress);
            if (!validIPAddress)
            {
                if (hostName == "localhost")
                {
                    ipAddress = IPAddress.Parse("127.0.0.1");
                }
                else
                {
                    IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);
                    ipAddress = ipHostInfo.AddressList[0];
                }
            }

            return ipAddress;
        }

        #endregion

    }

}

