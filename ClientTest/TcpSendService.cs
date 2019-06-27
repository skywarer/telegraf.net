using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TelegrafNet.Client;

namespace ClientTest
{
    public class TcpSendService : BackgroundService
    {
        private readonly IMetricSender _tgfClient;
        public TcpSendService(IMetricSender metricSender)
        {
            _tgfClient = metricSender;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() =>
            {
                _tgfClient.SendMetric("query_pass", "pass", 0.03d);
                Console.WriteLine("metric sended");
            });
        }
    }
}
