using System;
using Delobytes.Mapper;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Core.Entities;
using YA.ServiceTemplate.Application.Models.SaveModels;

namespace YA.ServiceTemplate.Application.Mappers
{
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
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Cylinders = source.Cylinders;
            destination.Brand = source.Brand;
            destination.Model = source.Model;
        }

        public void Map(CarSm source, Car destination)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

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
}
