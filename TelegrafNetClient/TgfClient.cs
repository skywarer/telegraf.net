using System;
using TelegrafNet.Client.Transport;

namespace TelegrafNet.Client
{
    public class TgfClient : IMetricSender
    {
        IMetricMessageSender transport;

        string[] tagKeysDef;

        string[] tagValuesDef;

        public TgfClient(IMetricMessageSender transport) {
            this.transport = transport;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="transport"></param>
        /// <param name="tagKeys">Список тегов по-умолчанию</param>
        /// <param name="tagValues">Список значений тегов по-умолчанию</param>
        public TgfClient(IMetricMessageSender transport, string[] tagKeys, string[] tagValues)
        {
            this.transport = transport;
            tagKeysDef = tagKeys;
            tagValuesDef = tagValues;
        }
        
        private MetricMessage CreateMessageWithDef(string measurement, string[] indicators, double[] values, string[] tagKeys, string[] tagValues) {
            string indicator = indicators[0];
            double value = values[0];
            MetricMessage msg = new MetricMessage(measurement, indicator, value);
            // дополняем индикаторы
            for (int i = 1; i < indicator.Length; i++)
            {
                if (i >= values.Length) break;
                if (string.IsNullOrEmpty(indicators[i])) { throw new ArgumentNullException("indicator is empty"); }
                msg.AddIndicatorValue(indicators[i], values[i]);
            }
            // доп.теги
            if (tagKeys != null && tagValues != null)
            {
                for (int i = 0; i < tagKeys.Length; i++)
                {
                    if (i >= tagValues.Length) break;
                    if (string.IsNullOrEmpty(tagKeys[i])) { throw new ArgumentNullException("tagName is empty"); }
                    if (string.IsNullOrEmpty(tagValues[i])) { throw new ArgumentNullException("tagValue is empty"); }
                    msg.AddTag(tagKeys[i], tagValues[i]);
                }
            }
            //теги поумолчанию
            if (tagKeysDef != null && tagValuesDef != null) {
                for (int i = 0; i < tagKeysDef.Length; i++)
                {
                    if (i >= tagValuesDef.Length) break;
                    if (string.IsNullOrEmpty(tagKeysDef[i])) { throw new ArgumentNullException("tagNameDef is empty"); }
                    if (string.IsNullOrEmpty(tagValuesDef[i])) { throw new ArgumentNullException("tagValueDef is empty"); }
                    msg.AddTag(tagKeysDef[i], tagValuesDef[i]);
                }
            }
            return msg;
        }

        public void SendMetric(string measurement, string indicator, double value)
        {
            MetricMessage msg = CreateMessageWithDef(measurement, new string[] { indicator }, new double[] { value }, null, null);
            transport.Send(msg);
        }

        public void SendMetric(string measurement, string indicator, double value, string tagKey, string tagValue)
        {
            MetricMessage msg = CreateMessageWithDef(measurement, new string[] { indicator }, new double[] { value }, new string[] { tagKey }, new string[] { tagValue });
            transport.Send(msg);
        }

        public void SendMetric(string measurement, string indicator, double value, string[] tagKeys, string[] tagValues)
        {
            MetricMessage msg = CreateMessageWithDef(measurement, new string[] { indicator }, new double[] { value }, tagKeys, tagValues);
            transport.Send(msg);
        }

        public void SendMetric(string measurement, string[] indicators, double[] values, string[] tagKeys, string[] tagValues)
        {
            MetricMessage msg = CreateMessageWithDef(measurement, indicators, values, tagKeys, tagValues);
            transport.Send(msg);
        }
    }
}
