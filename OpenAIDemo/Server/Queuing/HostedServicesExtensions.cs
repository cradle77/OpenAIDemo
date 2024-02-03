using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OpenAIDemo.Server.Queuing
{
    public static class HostedServicesExtensions
    {
        public static IServiceCollection AddQueueProcessor(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddHostedService<QueueProcessor>();
            services.TryAddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

            return services;
        }
    }
}
