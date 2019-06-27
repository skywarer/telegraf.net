using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace TelegrafNet.Client.Transport
{
    public class MetricUnixSender : IMetricMessageSender
    {
        public byte trailer { get; set; }
        protected string filename;
        protected Socket socket;
        protected Stream transportStream = null;

        public MetricUnixSender(string filename, bool shouldAutoConnect = true) {
            PlatformID platform = Environment.OSVersion.Platform;
            if (!(platform == PlatformID.MacOSX || platform == PlatformID.Unix))
            {
                throw new System.Net.WebException("MetricUnixSender is only available on Unix-like systems (e.g., Linux, BSD, OS X)");
            }
            this.filename = filename;
            if (shouldAutoConnect)
            {
                Connect();
            }
            trailer = 10; // LF
        }

        public void Connect()
        {
            try
            {
                socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                var uei = new UnixDomainSocketEndPoint(filename);
                socket.Connect(uei);
                transportStream = new NetworkStream(socket);
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        public virtual void Reconnect()
        {
            Disconnect();
            Connect();
        }

        public void Disconnect()
        {
            if (transportStream != null)
            {
                transportStream.Close();
                transportStream = null;
            }
            if (socket != null)
            {
                socket.Close();
                socket = null;
            }
        }

        public void Send(MetricMessage message)
        {
            Send(message, true);
        }

        protected void Send(MetricMessage message, bool flush = true)
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
    }
}
