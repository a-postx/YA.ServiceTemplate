using Delobytes.Mapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using YA.ServiceTemplate.Application.Models.ViewModels;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.Mappers;

/// <summary>
/// Mapper for mapping internal car object into view car object.
/// </summary>
public class CarToVmMapper : IMapper<Car, CarVm>
{
    public CarToVmMapper(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator)
    {
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
    }

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;

    public void Map(Car source, CarVm destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        destination.CarId = source.CarId;
        destination.Cylinders = source.Cylinders;
        destination.Brand = source.Brand;
        destination.Model = source.Model;
        destination.Created = source.Created;
        destination.Modified = source.Modified;
        //property name of anonymous route value object must correspond to controller http route values
        destination.Url = new Uri(_linkGenerator.GetUriByRouteValues(_httpContextAccessor.HttpContext, RouteNames.GetCar, new { source.CarId }));
    }
}
