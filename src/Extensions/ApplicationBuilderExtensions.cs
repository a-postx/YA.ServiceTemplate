using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Options;
using Delobytes.AspNetCore;
using YA.ServiceTemplate.Infrastructure.Logging.Requests;
using YA.ServiceTemplate.Application.Middlewares;

namespace YA.ServiceTemplate.Extensions
{
    internal static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseDeveloperErrorPages(this IApplicationBuilder application)
        {
            return application
                .UseDeveloperExceptionPage();
        }

        /// <summary>
        /// Uses the static files middleware to serve static files. Also adds the Cache-Control and Pragma HTTP
        /// headers. The cache duration is controlled from configuration.
        /// See http://andrewlock.net/adding-cache-control-headers-to-static-files-in-asp-net-core/.
        /// </summary>
        public static IApplicationBuilder UseStaticFilesWithCacheControl(this IApplicationBuilder application)
        {
            CacheProfile cacheProfile = application
                .ApplicationServices
                .GetRequiredService<CacheProfileOptions>()
                .Where(x => string.Equals(x.Key, CacheProfileName.StaticFiles, StringComparison.Ordinal))
                .Select(x => x.Value)
                .SingleOrDefault();

            application.UseStaticFiles(
                new StaticFileOptions()
                {
                    OnPrepareResponse = context =>
                    {
                        context.Context.ApplyCacheProfile(cacheProfile);
                    },
                });

            return application;
        }

        /// <summary>
        /// Добавляет прослойку для активации уникального пользовательского интерфейса Swagger
        /// </summary>
        public static IApplicationBuilder UseCustomSwaggerUI(this IApplicationBuilder application)
        {
            return application.UseSwaggerUI(options =>
                {
                    // Set the Swagger UI browser document title.
                    options.DocumentTitle = typeof(Startup).Assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
                    // Set the Swagger UI to render at '/'.
                    ////options.RoutePrefix = string.Empty;
                    
                    options.DisplayOperationId();
                    options.DisplayRequestDuration();

                    IApiVersionDescriptionProvider provider = application.ApplicationServices.GetService<IApiVersionDescriptionProvider>();

                    foreach (ApiVersionDescription apiVersionDescription in provider.ApiVersionDescriptions.OrderByDescending(x => x.ApiVersion))
                    {
                        options.SwaggerEndpoint(
                            $"/swagger/{apiVersionDescription.GroupName}/swagger.json",
                            $"Version {apiVersionDescription.ApiVersion}");
                    }
                });
        }

        /// <summary>
        /// Добавляет прослойку логирования сетевого контекста
        /// </summary>
        public static IApplicationBuilder UseNetworkContextLogging(this IApplicationBuilder application)
        {
            return application
                .UseMiddleware<NetworkContextLogger>();
        }

        /// <summary>
        /// Добавляет прослойку логирования контекста HTTP
        /// </summary>
        public static IApplicationBuilder UseHttpContextLogging(this IApplicationBuilder application)
        {
            return application
                .UseMiddleware<HttpContextLogger>();
        }

        /// <summary>
        /// Добавляет прослойку перехвата исключения в HTTP-контексте. Выводит детали проблемы с дополнительным контекстом.
        /// Внимание! При отключении этой прослойки все необработанные исключения станут уходить в контекст MVC,
        /// перестанут выдавать <see cref="ProblemDetails"/> и логироваться в событии завершения запроса.
        /// </summary>
        public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder application)
        {
            return application
                .UseMiddleware<HttpExceptionHandler>();
        }
    }
}
