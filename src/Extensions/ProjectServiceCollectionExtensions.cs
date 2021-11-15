using Delobytes.Mapper;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using YA.ServiceTemplate.Application.ActionHandlers.Cars;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Mappers;
using YA.ServiceTemplate.Application.Models.SaveModels;
using YA.ServiceTemplate.Application.Models.ViewModels;
using YA.ServiceTemplate.Application.Services;
using YA.ServiceTemplate.Core.Entities;
using YA.ServiceTemplate.Infrastructure.Data;
using YA.ServiceTemplate.Infrastructure.Messaging;
using YA.ServiceTemplate.Infrastructure.Services;

namespace YA.ServiceTemplate.Extensions;

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
    /// Add available action handlers to the service collection.
    /// </summary>
    public static IServiceCollection AddProjectActionHandlers(this IServiceCollection services)
    {
        return services
            .AddScoped<IGetCarAh, GetCarAh>()
            .AddScoped<IGetCarPageAh, GetCarPageAh>()
            .AddScoped<IPostCarAh, PostCarAh>()
            .AddScoped<IPutCarAh, PutCarAh>()
            .AddScoped<IPatchCarAh, PatchCarAh>()
            .AddScoped<IDeleteCarAh, DeleteCarAh>();
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
            .AddScoped<IPaginatedResultFactory, PaginatedResultFactory>()
            .AddScoped<IMessageBus, MessageBus>();
    }

    /// <summary>
    /// Добавляет кастомизированную фабрику Деталей Проблемы.
    /// </summary>
    public static IServiceCollection AddCustomProblemDetails(this IServiceCollection services)
    {
        services
            .AddTransient<IProblemDetailsFactory, YaProblemDetailsFactory>()
            .AddTransient<ProblemDetailsFactory, YaProblemDetailsFactory>();

        return services;
    }
}
