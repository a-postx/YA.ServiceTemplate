using Delobytes.Mapper;
using Microsoft.Extensions.DependencyInjection;
using YA.ServiceTemplate.Application.Commands;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Mappers;
using YA.ServiceTemplate.Application.Models.SaveModels;
using YA.ServiceTemplate.Application.Models.ViewModels;
using YA.ServiceTemplate.Core.Entities;
using YA.ServiceTemplate.Infrastructure.Data;
using YA.ServiceTemplate.Infrastructure.Services;

namespace YA.ServiceTemplate
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods add project services.
    /// </summary>
    /// <remarks>
    /// AddSingleton - Only one instance is ever created and returned.
    /// AddScoped - A new instance is created and returned for each request/response cycle.
    /// AddTransient - A new instance is created and returned each time.
    /// </remarks>
    public static class ProjectServiceCollectionExtensions
    {
        /// <summary>
        /// Add available commands to the service collection.
        /// </summary>
        public static IServiceCollection AddProjectCommands(this IServiceCollection services)
        {
            return services
                .AddSingleton<IDeleteCarCommand, DeleteCarCommand>()
                .AddSingleton<IGetCarCommand, GetCarCommand>()
                .AddSingleton<IGetCarPageCommand, GetCarPageCommand>()
                .AddSingleton<IPatchCarCommand, PatchCarCommand>()
                .AddSingleton<IPostCarCommand, PostCarCommand>()
                .AddSingleton<IPutCarCommand, PutCarCommand>();
        }

        /// <summary>
        /// Add project mappers to the service collection.
        /// </summary>
        public static IServiceCollection AddProjectMappers(this IServiceCollection services)
        {
            return services
                .AddSingleton<IMapper<Car, CarVm>, CarToVmMapper>()
                .AddSingleton<IMapper<Car, CarSm>, CarToSmMapper>()
                .AddSingleton<IMapper<CarSm, Car>, CarToSmMapper>();
        }

        /// <summary>
        /// Add repositories to the service collection.
        /// </summary>
        public static IServiceCollection AddProjectRepositories(this IServiceCollection services)
        {
            return services.AddSingleton<IAppRepository, AppRepository>();
        }

        /// <summary>
        /// Add internal services to the service collection.
        /// </summary>
        public static IServiceCollection AddProjectServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<IClockService, ClockService>()
                .AddSingleton<IRuntimeGeoDataService, IpWhoisRuntimeGeoData>()
                .AddScoped<IRuntimeContextAccessor, RuntimeContextAccessor>()
                .AddHostedService<StartupService>()
                .AddHostedService<MessageBusService>();
        }
    }
}
