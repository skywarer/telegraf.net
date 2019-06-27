using System.Collections.Generic;
using System.Net.Sockets;

namespace TelegrafNet.Client.Transport
{
    public class MetricUdpSender : IMetricMessageSender
    {
        protected readonly UdpClient udpClient;

        public MetricUdpSender(string hostname, int port) {
            udpClient = new UdpClient(hostname, port);
        }

        public void Disconnect()
        {
            if (udpClient != null && udpClient.Client!= null && udpClient.Client.Connected)
            {
                udpClient.Close();
            }
        }

        public void Reconnect()
        {
            
        }

        public void Send(MetricMessage message)
        {
            byte[] datagramBytes = message.ByteSerialize();
            udpClient.Send(datagramBytes, datagramBytes.Length);
        }

        public void Send(IEnumerable<MetricMessage> messages)
        {
            foreach (MetricMessage message in messages)
            {
                Send(message);
            }
        }
    }
}
