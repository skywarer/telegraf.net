using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace TelegrafNet.Client.Transport
{
    public class MetricTcpSender : IMetricMessageSender
    {
        protected string hostname;
        protected int port;

        protected TcpClient tcpClient = null;
        protected Stream transportStream = null;
        public byte trailer { get; set; }

        public MetricTcpSender(string hostname, int port, bool shouldAutoConnect = true)
        {
            this.hostname = hostname;
            this.port = port;

            if (shouldAutoConnect)
            {
                Connect();
            }
            trailer = 10; // LF
        }

        public virtual void Connect()
        {
            try
            {
                tcpClient = new TcpClient(hostname, port);
                transportStream = tcpClient.GetStream();
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        public void Disconnect()
        {
            if (transportStream != null)
            {
                transportStream.Close();
                transportStream = null;
            }

            if (tcpClient != null)
            {
                tcpClient.Close();
                tcpClient = null;
            }
        }
        
        public void Reconnect()
        {
            Disconnect();
            Connect();
        }

        public void Send(MetricMessage message, bool flush = true)
        {
            if (transportStream == null)
            {
                throw new IOException("No transport stream exists");
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                var datagramBytes = message.ByteSerialize();
                memoryStream.Write(datagramBytes, 0, datagramBytes.Length);
                memoryStream.WriteByte(trailer); // LF
                transportStream.Write(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
            }

            if (flush && !(transportStream is NetworkStream))
                transportStream.Flush();
        }

        public void Send(IEnumerable<MetricMessage> messages)
        {
            foreach (MetricMessage message in messages)
            {
                Send(message, false);
            }
            if (!(transportStream is NetworkStream))
                transportStream.Flush();
        }

        public void Send(MetricMessage message)
        {
            Send(message, true);
        }
    }
}
