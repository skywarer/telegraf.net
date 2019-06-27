using System;
using System.Collections.Generic;
using System.Text;

namespace TelegrafNet.Client.Transport
{
    public interface IMetricMessageSender
    {
        void Reconnect();
        void Send(MetricMessage message);
        void Send(IEnumerable<MetricMessage> messages);
        void Disconnect();

    }
}
