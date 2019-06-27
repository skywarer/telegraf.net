using System;
using System.Collections.Generic;
using System.Text;

namespace TelegrafNet.Client
{
    public interface IMetricSender
    {
        void SendMetric(string measurement, string indicator, double value);

        void SendMetric(string measurement, string indicator, double value, string tagKey, string tagValue);

        void SendMetric(string measurement, string indicator, double value, string[] tagKeys, string[] tagValues);

        void SendMetric(string measurement, string[] indicators, double[] values, string[] tagKeys, string[] tagValues);

    }
}
