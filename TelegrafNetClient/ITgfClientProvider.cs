using System;

namespace TelegrafNet.Client
{
    public interface ITgfClientProvider : IDisposable
    {
        IMetricSender CreateMetricSender();
    }
}