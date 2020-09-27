using GreenPipes;
using System;
using YA.ServiceTemplate.Infrastructure.Messaging.Filters;

namespace YA.ServiceTemplate.Extensions
{
    public static class MassTransitPipeConfiguratorExtensions
    {
        /// <summary>
        /// Вставляет в конвеер фильтр для забора уникального контекста из сообщения МассТранзита.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configurator"></param>
        public static void UseMbContextFilter<T>(this IPipeConfigurator<T> configurator) where T : class, PipeContext
        {
            if (configurator == null)
                throw new ArgumentNullException(nameof(configurator));

            configurator.AddPipeSpecification(new MbMessageContextFilterPipeSpecification<T>());
        }
    }
}
