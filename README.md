# telegraf.net
send metrics from net.core application
Example:
```c#
string address = "tcp://localhost:14000"
IMetricMessageSender transport = TgfClientProvider.GetTransport(address);
TgfClient client = new TgfClient(transport);
client.SendMetric("mymeasure", "ping", 0.03d);
``` 
Dependency Injection:  
`appsettings.json`
```json
{
  "Telegraf": {
    "address": "unixgram:///tmp/telegraf.sock",
    "tag_keys": [ "appname", "apphost" ], // default tags one to one
    "tag_values": [ "myapp", "myhost" ] 
  }
}
```
`Program.cs` 
```c# 
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TelegrafNetClient;
... 
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
```
`TcpSendService.cs`
```c#
...
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
                _tgfClient.SendMetric("mymeasure", "ping", 0.03d); 
                Console.WriteLine("metric sended");
            });
        } 
    }
```
Simple telegraf settings

```toml
[[inputs.socket_listener]]
        service_address = "unixgram:///tmp/telegraf.sock"
        data_format = "json"
        json_name_key = "mymeasure" # requre
        tag_keys = ["appname", "apphost"]
```  
aggregation:  
```toml
[[aggregators.basicstats]]
        period = "10s"
        drop_original = true
        stats = ["mean"]
        namepass = ["mymeasure"]
        fieldpass = ["ping"]
```
Output file:  
```toml
[[outputs.file]]
        files = ["/tmp/metrics.out"]
        data_format = "json"
        json_timestamp_units = "1s"
```
