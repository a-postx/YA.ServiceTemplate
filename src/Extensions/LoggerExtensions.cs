using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace YA.ServiceTemplate.Extensions
{
    public static class LoggerExtensions
    {
        ///<summary>Обрамляет контекст логирования дополнительными параметрами.</summary>
        /// <param name="logger">Логер.</param>
        ///<param name="paramsAndValues">Параметры и их значения, которые нужно добавить в контекст.</param>
        public static IDisposable BeginScopeWith(this ILogger logger, params (string key, object value)[] paramsAndValues)
        {
            return logger.BeginScope(paramsAndValues.ToDictionary(x => x.key, x => x.value));
        }
    }
}
