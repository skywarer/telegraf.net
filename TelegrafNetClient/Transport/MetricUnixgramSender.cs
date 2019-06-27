using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace TelegrafNet.Client.Transport
{
    public class MetricUnixgramSender : IMetricMessageSender
    {
        private readonly Socket socket;

        public MetricUnixgramSender(string filename) {
            PlatformID platform = Environment.OSVersion.Platform;
            if (!(platform == PlatformID.MacOSX || platform == PlatformID.Unix))
            {
                throw new System.Net.WebException("MetricUnixgramSender is only available on Unix-like systems (e.g., Linux, BSD, OS X)");
            }
            socket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.Unspecified);
            var uei = new UnixDomainSocketEndPoint(filename);//new UnixEndPoint(filename); //
            socket.Connect(uei);
        }
        public void Disconnect()
        {
            if (socket != null && socket.Connected)
            {
                socket.Close();
            }
        }

        public void Reconnect()
        {

        }

        public void Send(MetricMessage message)
        {
            byte[] datagramBytes = message.ByteSerialize();
            socket.Send(datagramBytes);
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
