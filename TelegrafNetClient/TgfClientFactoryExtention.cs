using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace TelegrafNet.Client
{
    public static class TgfClientFactoryExtentions
    {
        public static IServiceCollection AddTgfClient(this IServiceCollection services, string address)
        {
            services.AddSingleton<ITgfClientProvider>(new TgfClientProvider(address));
            services.AddTransient(sp =>
            {
                var p = sp.GetService<ITgfClientProvider>();
                if (p != null)
                {
                    return p.CreateMetricSender();
                }
                else
                {
                    return null;
                }
            });
            services.AddScoped(sp =>
            {
                var p = sp.GetService<ITgfClientProvider>();
                if (p != null)
                {
                    return p.CreateMetricSender();
                }
                else {
                    return null;
                }
            });

            return services;
        }

        public static IServiceCollection AddTgfClient(this IServiceCollection services, string address, string[] tagKeysDef, string[] tagValuesDef)
        {
            services.AddSingleton<ITgfClientProvider>(new TgfClientProvider(address, tagKeysDef, tagValuesDef));
            services.AddTransient(sp =>
            {
                var p = sp.GetService<ITgfClientProvider>();
                if (p != null)
                {
                    return p.CreateMetricSender();
                }
                else
                {
                    return null;
                }
            });
            services.AddScoped(sp =>
            {
                var p = sp.GetService<ITgfClientProvider>();
                if (p != null)
                {
                    return p.CreateMetricSender();
                }
                else
                {
                    return null;
                }
            });
            return services;
        }

        public static IServiceCollection AddTgfClient(this IServiceCollection services, IConfiguration config, string root = "Telegraf") {
            string address = config[root + ":address"];
            string[] tagKeysDef = config.GetSection(root + ":tag_keys").GetChildren().Select(x => x.Value).ToArray();
            string[] tagValuesDef = config.GetSection(root + ":tag_values").GetChildren().Select(x => x.Value).ToArray();
            return AddTgfClient(services,address, tagKeysDef, tagValuesDef);
        }
    }
}
