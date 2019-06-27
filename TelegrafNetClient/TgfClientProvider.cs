using System;
using TelegrafNet.Client.Transport;

namespace TelegrafNet.Client
{
    public class TgfClientProvider : ITgfClientProvider, IDisposable
    {

        string[] _tagNames;
        string[] _tagValues;

        readonly IMetricMessageSender transport;

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <param name="address"></param>
        public TgfClientProvider(string address)
        {
            transport = GetTransport(address);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <param name="address"></param>
        public TgfClientProvider(string address, string[] tagKeys, string[] tagValues)
        {
            transport = GetTransport(address);
            _tagNames = tagKeys;
            _tagValues = tagValues;
        }

        public static IMetricMessageSender GetTransport(string address)
        {
            IMetricMessageSender transport = null;
            Uri uri = new Uri(address);
            string scheme = uri.Scheme;
            switch (scheme)
            {
                case "tcp":
                    transport = new MetricTcpSender(uri.Host, uri.Port);
                    break;
                case "udp":
                    transport = new MetricUdpSender(uri.Host, uri.Port);
                    break;
                case "unix":
                    transport = new MetricUnixSender(uri.AbsolutePath);
                    break;
                case "unixgram":
                    transport = new MetricUnixgramSender(uri.AbsolutePath);
                    break;
                default:
                    throw new UriFormatException($"not allowed scheme {scheme}");
            }

            return transport;
        }

        public IMetricSender CreateMetricSender() {
            return (_tagNames != null && _tagValues != null) ? new TgfClient(transport, _tagNames, _tagValues) : new TgfClient(transport);
        }

        public void Dispose()
        {
            if (transport != null)
                transport.Disconnect();
        }
    }
}
