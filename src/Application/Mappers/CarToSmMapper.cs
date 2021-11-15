using Delobytes.Mapper;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.SaveModels;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.Mappers;

/// <summary>
/// Mapper for mapping internal car object into savecar and vice versa.
/// </summary>
public class CarToSmMapper : IMapper<Car, CarSm>, IMapper<CarSm, Car>
{
    private readonly IClockService _clockService;

    public CarToSmMapper(IClockService clockService)
    {
        _clockService = clockService;
    }

    public void Map(Car source, CarSm destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        destination.Cylinders = source.Cylinders;
        destination.Brand = source.Brand;
        destination.Model = source.Model;
    }

    public void Map(CarSm source, Car destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        DateTimeOffset now = _clockService.UtcNow;

        if (destination.Created == DateTimeOffset.MinValue)
        {
            destination.Created = now;
        }

        destination.Cylinders = source.Cylinders;
        destination.Brand = source.Brand;
        destination.Model = source.Model;
        destination.Modified = now;
    }
}
