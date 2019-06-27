using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NDesk.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TelegrafNet.Client;

namespace ClientTest
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            try
            {
                bool test = false;
                bool help = false;
                string hostaddr = null;
                string name = "measurement_name";
                var p = new OptionSet();
                List<string> tags = new List<string>();
                List<string> inds = new List<string>();
                p.Add("e|test", () => "Test all transport type: tcp://127.0.0.1:14230 udp://127.0.0.1:14230 unix:///tmp/tcp.sock " +
                "unixgram://tmp/udp.sock",
                    v => test = v != null);
                p.Add("a|hostaddress=", () => "Address like udp://127.0.0.1:14230 unix:///tmp/tcp.sock", v => hostaddr = v);
                p.Add("t|tag=", () => "tagKey:tagValue", v => tags.Add(v));
                p.Add("i|indicator=", () => "indKey:indValue", v => inds.Add(v));
                p.Add("m|measurement=", () => "Measurement name", v => name = v);
                p.Add("h|help", v => help = v != null);
                List<string> extra;
                try
                {
                    extra = p.Parse(args);
                }
                catch (OptionException e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }
                inds.AddRange(extra);
                if (help)
                {
                    ShowHelp(p);
                }
                else if (test)
                {
                    await TestSendAsync();
                    Console.WriteLine("press any key");
                    Console.ReadKey();
                }
                else
                {
                    Send(name, hostaddr, inds, tags);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("press any key");
                Console.ReadKey();
            }
        }

        private static void Send(string measurement, string address, List<string> inds, List<string> tags)
        {
            string[] tagKeys = null, tagValues = null, indKeys = null;
            double[] indValues = null;
            if (tags.Count > 0)
            {
                tagKeys = new string[tags.Count];
                tagValues = new string[tags.Count];
                int i = 0;
                foreach (var s in tags)
                {
                    var tg = s.Split(':');
                    if (tg.Length > 1)
                    {
                        tagKeys[i] = tg[0];
                        tagValues[i] = tg[1];
                        i++;
                    }
                }
            }
            if (inds.Count > 0)
            {
                indKeys = new string[inds.Count];
                indValues = new double[inds.Count];
                foreach (var s in inds)
                {
                    int i = 0;
                    var ind = s.Split(':');
                    if (ind.Length > 1 && double.TryParse(ind[1], out double v))
                    {
                        indKeys[i] = ind[0];
                        indValues[i] = v;
                        i++;
                    }
                }
            }
            var transport = TgfClientProvider.GetTransport(address);
                TgfClient client = new TgfClient(transport);
            {
                client.SendMetric(measurement, indKeys, indValues, tagKeys, tagValues);
            }
            transport.Disconnect();
        }

        private static async System.Threading.Tasks.Task TestSendAsync()
        {
            TestMessagePacking();
            await TestTcpSendAsync();
            await TestUdpSendAsync();
            await TestUnixSendAsync();
            await TestUnixgramSendAsync();
            await TestDIAsync();
            Console.WriteLine("All test passed");
        }

        private static void TestMessagePacking()
        {
            string sample = "{\"name\":\"mymeasure\",\"ping\":\"0.03\"}";
            MetricMessage msg = new MetricMessage("mymeasure", "ping", 0.03d);
            if (!sample.Equals(msg.ToString())) throw new Exception($"TestMessagePacking: {msg.ToString()} not equal {sample}");
            sample = "{\"name\":\"mymeasure\",\"ping\":\"0.03\",\"mytag\":\"old\"}";
            msg.AddTag("mytag", "old");
            if (!sample.Equals(msg.ToString())) throw new Exception($"TestMessagePacking: {msg.ToString()} not equal {sample}");
            JObject o = JObject.Parse(sample);
        }

        private static async System.Threading.Tasks.Task TestTcpSendAsync()
        {
            Console.WriteLine("TestTcpSendAsync start:");
            int port = 14230;
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);
            string address = $"tcp://{ipAddr}:{port}";
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(ipEndPoint);
                socket.Listen(1);
                var acceptTask = socket.AcceptAsync();
                MetricMessage msg = new MetricMessage("mymeasure", "ping", 0.03d);
                string sample = msg.ToString();
                var transport = TgfClientProvider.GetTransport(address);
                TgfClient client = new TgfClient(transport);
                client.SendMetric("mymeasure", "ping", 0.03d);
                transport.Disconnect();
                var handler = await acceptTask;
                byte[] bytes = new byte[1024];
                int bytesRec = handler.Receive(bytes);
                string msgRcv = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                if (!sample.Equals(msgRcv.Trim())) throw new Exception($"TestTcpSendAsync: {msgRcv} not equal {sample}");
            }
            Console.WriteLine("TestTcpSendAsync end");
        }

        private static async System.Threading.Tasks.Task TestUdpSendAsync()
        {
            Console.WriteLine("TestUdpSendAsync start:");
            int port = 14231;
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);
            string address = $"udp://{ipAddr}:{port}";
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Bind(ipEndPoint);
                byte[] bytes = new byte[1024];
                var bytesRecTask = socket.ReceiveAsync(bytes, SocketFlags.None);
                MetricMessage msg = new MetricMessage("mymeasure", "ping", 0.03d);
                string sample = msg.ToString();
                var transport = TgfClientProvider.GetTransport(address);
                TgfClient client = new TgfClient(transport);
                client.SendMetric("mymeasure", "ping", 0.03d);
                transport.Disconnect();
                int bytesRec = await bytesRecTask;
                string msgRcv = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                if (!sample.Equals(msgRcv.Trim())) throw new Exception($"TestUdpSendAsync: {msgRcv} not equal {sample}");
            }
            Console.WriteLine("TestUdpSendAsync end");
        }

        private static async System.Threading.Tasks.Task TestUnixSendAsync()
        {
            Console.WriteLine("TestUnixSendAsync start:");
            string filepath = "/tmp/tcp.sock";
            EndPoint ipEndPoint = new UnixDomainSocketEndPoint(filepath);
            string address = $"unix://{filepath}";
            try { File.Delete(filepath); } catch (Exception) { };
            using (Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
            {
                socket.Bind(ipEndPoint);
                socket.Listen(1);
                var acceptTask = socket.AcceptAsync();
                MetricMessage msg = new MetricMessage("mymeasure", "ping", 0.03d);
                string sample = msg.ToString();
                var transport = TgfClientProvider.GetTransport(address);
                TgfClient client = new TgfClient(transport);
                client.SendMetric("mymeasure", "ping", 0.03d);
                transport.Disconnect();
                var handler = await acceptTask;
                byte[] bytes = new byte[1024];
                int bytesRec = handler.Receive(bytes);
                string msgRcv = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                if (!sample.Equals(msgRcv.Trim())) throw new Exception($"TestUnixSendAsync: {msgRcv} not equal {sample}");
            }
            try { File.Delete(filepath); } catch (Exception) { };
            Console.WriteLine("TestUnixSendAsync end");
        }

        private static async System.Threading.Tasks.Task TestUnixgramSendAsync()
        {
            Console.WriteLine("TestUnixgramSendAsync start:");
            string filepath = "/tmp/udp.sock";
            EndPoint ipEndPoint = new UnixDomainSocketEndPoint(filepath);
            string address = $"unixgram://{filepath}";
            try { File.Delete(filepath); } catch (Exception) { };
            using (Socket socket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.Unspecified))
            {
                socket.Bind(ipEndPoint);
                byte[] bytes = new byte[1024];
                var bytesRecTask = socket.ReceiveAsync(bytes, SocketFlags.None);
                MetricMessage msg = new MetricMessage("mymeasure", "ping", 0.03d);
                string sample = msg.ToString();
                var transport = TgfClientProvider.GetTransport(address);
                TgfClient client = new TgfClient(transport);
                client.SendMetric("mymeasure", "ping", 0.03d);
                transport.Disconnect();
                int bytesRec = await bytesRecTask;
                string msgRcv = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                if (!sample.Equals(msgRcv.Trim())) throw new Exception($"TestUnixgramSendAsync: {msgRcv} not equal {sample}");
            }
            try { File.Delete(filepath); } catch (Exception) { };
            Console.WriteLine("TestUnixgramSendAsync end");
        }

        private static async System.Threading.Tasks.Task TestDIAsync()
        {
            Console.WriteLine("TestDIAsync start:");
            int port = 14230;
            IPHostEntry ipHost = Dns.GetHostEntry("localhost");
            IPAddress ipAddr = ipHost.AddressList[1];
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, port);
            string address = $"tcp://{ipAddr}:{port}";
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(ipEndPoint);
                socket.Listen(1);
                var acceptTask = socket.AcceptAsync();
                MetricMessage msg = new MetricMessage("mymeasure", "ping", 0.03d);
                string sample = msg.ToString();
                Console.WriteLine("...start host:");
                if (!File.Exists("appsettings.json"))
                {
                    Console.WriteLine("ERR: {0} not found", "appsettings.json");
                }
                var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTgfClient(hostContext.Configuration);
                    services.AddHostedService<TcpSendService>();
                });
                builder.Build().Run();
                // wait ctrl+c
                Console.WriteLine("...host finished:");
                var handler = await acceptTask;
                byte[] bytes = new byte[1024];
                int bytesRec = handler.Receive(bytes);
                string msgRcv = Encoding.UTF8.GetString(bytes, 0, bytesRec);
                if (!sample.Equals(msgRcv.Trim())) throw new Exception($"TestDIAsync: {msgRcv} not equal {sample}");
            }
            Console.WriteLine("TestDIAsync end");
        }

        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: [OPTIONS] + message");
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }
    }
}
