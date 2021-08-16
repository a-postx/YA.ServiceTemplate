using Delobytes.AspNetCore;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using YA.ServiceTemplate.Options;

namespace YA.ServiceTemplate.Extensions
{
    internal static class MvcBuilderExtensions
    {
        /// <summary>
        /// Добавляет кастомизированные настройки сериализации JSON.
        /// </summary>
        public static IMvcBuilder AddCustomJsonOptions(this IMvcBuilder builder, IWebHostEnvironment webHostEnvironment)
        {
            return builder.AddJsonOptions(options =>
            {
                JsonSerializerOptions jsonSerializerOptions = options.JsonSerializerOptions;
                if (webHostEnvironment.IsDevelopment())
                {
                    // Pretty print the JSON in development for easier debugging.
                    jsonSerializerOptions.WriteIndented = true;
                }

                jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                jsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                jsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                jsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
            });
        }

        /// <summary>
        /// Добавляет кастомизированные настройки MVC.
        /// </summary>
        public static IMvcBuilder AddCustomMvcOptions(this IMvcBuilder builder, IConfiguration configuration)
        {
            return builder.AddMvcOptions(options =>
            {
                // Controls how controller actions cache content from the appsettings.json file.
                foreach (var keyValuePair in configuration
                    .GetSection(nameof(ApplicationOptions.CacheProfiles))
                    .Get<CacheProfileOptions>())
                {
                    options.CacheProfiles.Add(keyValuePair);
                }

                // Remove plain text (text/plain) output formatter.
                options.OutputFormatters.RemoveType<StringOutputFormatter>();

                // Configure System.Text JSON.
                MediaTypeCollection jsonSystemInputFormatterMediaTypes = options
                        .InputFormatters
                        .OfType<SystemTextJsonInputFormatter>()
                        .First()
                        .SupportedMediaTypes;
                MediaTypeCollection jsonSystemOutputFormatterMediaTypes = options
                    .OutputFormatters
                    .OfType<SystemTextJsonOutputFormatter>()
                    .First()
                    .SupportedMediaTypes;

                // Remove JSON text (text/json) media type from the system JSON input and output formatters.
                jsonSystemInputFormatterMediaTypes.Remove("text/json");
                jsonSystemOutputFormatterMediaTypes.Remove("text/json");

                // Add RESTful JSON media type (application/vnd.restful+json) to the JSON input and output formatters.
                // See http://restfuljson.org/
                jsonSystemInputFormatterMediaTypes.Insert(0, ContentType.RestfulJson);
                jsonSystemOutputFormatterMediaTypes.Insert(0, ContentType.RestfulJson);

                // Add Problem Details media type (application/problem+json) to the JSON output formatters.
                // See https://tools.ietf.org/html/rfc7807
                jsonSystemOutputFormatterMediaTypes.Insert(0, ContentType.ProblemJson);

                // Add support for Newtonsoft JSON Patch (application/json-patch+json).
                options.InputFormatters.Insert(0, GetJsonPatchInputFormatter());

                MediaTypeCollection jsonNewtonsoftInputFormatterMediaTypes = options
                        .InputFormatters
                        .OfType<NewtonsoftJsonInputFormatter>()
                        .First()
                        .SupportedMediaTypes;

                jsonNewtonsoftInputFormatterMediaTypes.Remove("text/json");
                jsonNewtonsoftInputFormatterMediaTypes.Insert(0, ContentType.RestfulJson);

                // Returns a 406 Not Acceptable if the MIME type in the Accept HTTP header is not valid.
                options.ReturnHttpNotAcceptable = true;
            });
        }

        /// <summary>
        /// Gets the JSON patch input formatter. The <see cref="JsonPatchDocument{T}"/> does not support the new
        /// System.Text.Json API's for de-serialization. You must use Newtonsoft.Json instead. See
        /// https://docs.microsoft.com/en-us/aspnet/core/web-api/jsonpatch?view=aspnetcore-3.0#jsonpatch-addnewtonsoftjson-and-systemtextjson
        /// </summary>
        /// <returns>The JSON patch input formatter using Newtonsoft.Json.</returns>
        private static NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter()
        {
            IServiceCollection services = new ServiceCollection()
                .AddLogging()
                .AddMvc()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    options.SerializerSettings.DateParseHandling = DateParseHandling.DateTimeOffset;
                })
                .Services;
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            MvcOptions mvcOptions = serviceProvider.GetRequiredService<IOptions<MvcOptions>>().Value;
            return mvcOptions.InputFormatters
                .OfType<NewtonsoftJsonPatchInputFormatter>()
                .First();
        }

        /// <summary>
        /// Добавляет кастомизированные настройки валидации моделей и поведения АПИ.
        /// </summary>
        public static IMvcBuilder AddCustomModelValidation(this IMvcBuilder builder)
        {
            return builder
                .AddFluentValidation(fv =>
                {
                    fv.RegisterValidatorsFromAssemblyContaining<Startup>();
                    fv.DisableDataAnnotationsValidation = true;
                    fv.ImplicitlyValidateChildProperties = true;
                    fv.LocalizationEnabled = true;
                    fv.ValidatorOptions.LanguageManager.Culture = new CultureInfo("ru");
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressMapClientErrors = true;
                    //фабрика Деталей Проблемы использует данные для добавления в недооформленные сущности 
                    options.ClientErrorMapping[400].Title = "Плохой запрос";
                    options.ClientErrorMapping[400].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/400";
                    options.ClientErrorMapping[401].Title = "Неавторизован";
                    options.ClientErrorMapping[401].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/401";
                    options.ClientErrorMapping[403].Title = "Запрещено";
                    options.ClientErrorMapping[403].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/403";
                    options.ClientErrorMapping[404].Title = "Не найдено";
                    options.ClientErrorMapping[404].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/404";
                    options.ClientErrorMapping[406].Title = "Неприемлемо";
                    options.ClientErrorMapping[406].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/406";
                    options.ClientErrorMapping[409].Title = "Конфликт";
                    options.ClientErrorMapping[409].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/409";
                    options.ClientErrorMapping[415].Title = "Не поддерживаемый тип содержимого";
                    options.ClientErrorMapping[415].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/415";
                    options.ClientErrorMapping[422].Title = "Необрабатываемая сущность";
                    options.ClientErrorMapping[422].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/422";
                    options.ClientErrorMapping[500].Title = "Внутренняя ошибка сервера.";
                    options.ClientErrorMapping[500].Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/500";

                    options.ClientErrorMapping[412] = new ClientErrorData
                    {
                        Title = "Предусловие не выполнено",
                        Link = "https://wiki.developer.mozilla.org/ru/docs/Web/HTTP/Status/412",
                    };

        });
        }
    }
}
