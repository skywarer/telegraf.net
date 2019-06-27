using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TelegrafNet.Client
{
    public class MetricMessage
    {
        public string Measurement { get; private set; }
        private List<KeyValuePair<string, string>> Tags { get; }
        private List<KeyValuePair<string, string>> Metrics { get; }
        
        public MetricMessage(string name, string indicator, double value)
        {
            Measurement = name ?? throw new ArgumentNullException("Measurement name is null");
            if(string.IsNullOrEmpty(indicator)) throw new ArgumentNullException("Indicator name is null");
            Tags = new List<KeyValuePair<string, string>>();
            Metrics = new List<KeyValuePair<string, string>>();
            AddIndicatorValue(indicator, value);
        }

        public void AddIndicatorValue(string indicator, double value)
        {
            if (string.IsNullOrEmpty(indicator)) throw new ArgumentNullException("Indicator name is null");
            Metrics.RemoveAll(x => x.Key.Equals(indicator));
            Metrics.Add(new KeyValuePair<string, string>(indicator, value.ToString("0.#####", System.Globalization.CultureInfo.InvariantCulture)));
        }

        public void AddTag(string tag, string value)
        {
            if (string.IsNullOrEmpty(tag)) throw new ArgumentNullException("Tag key is null");
            Tags.RemoveAll(x => x.Key.Equals(tag));
            Tags.Add(new KeyValuePair<string, string>(tag, value));
        }

        private string EscapeMsg(string msg)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char b in msg)
            {
                if (b.Equals('\n'))
                {
                    sb.Append('\\').Append('n');
                }
                else
                if (b.Equals('\r'))
                {
                    sb.Append('\\').Append('r');
                }
                else
                {
                    sb.Append(b);
                }
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            string tags = Tags.Any() ? string.Join(",", Tags.Select(x => $"\"{x.Key}\":\"{x.Value}\"")) : null;
            string metrics = string.Join(",", Metrics.Select(x => $"\"{x.Key}\":{x.Value}"));
            string msg = (tags == null) ? $"{{\"name\":\"{Measurement}\",{metrics}}}" 
                : $"{{\"name\":\"{Measurement}\",{metrics},{tags}}}";
            return EscapeMsg(msg);
        }

        public byte[] ByteSerialize() {
            return Encoding.UTF8.GetBytes(ToString());
        }
    }
}
