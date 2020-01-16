using Delobytes.Mapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.SaveModels;
using YA.ServiceTemplate.Application.Models.ViewModels;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.Commands
{
    public class PostCarCommand : IPostCarCommand
    {
        public PostCarCommand(ILogger<PostCarCommand> logger, IAppRepository carRepository, IMapper<Car, CarVm> carToCarVmMapper, IMapper<CarSm, Car> carSmToCarMapper)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
            _carToCarVmMapper = carToCarVmMapper ?? throw new ArgumentNullException(nameof(carToCarVmMapper));
            _carSmToCarMapper = carSmToCarMapper ?? throw new ArgumentNullException(nameof(carSmToCarMapper));
        }

        private readonly ILogger<PostCarCommand> _log;
        private readonly IAppRepository _carRepository;
        private readonly IMapper<Car, CarVm> _carToCarVmMapper;
        private readonly IMapper<CarSm, Car> _carSmToCarMapper;

        public async Task<IActionResult> ExecuteAsync(CarSm carSm, CancellationToken cancellationToken)
        {
            Car car = _carSmToCarMapper.Map(carSm);
            car = await _carRepository.AddAsync(car, cancellationToken);
            CarVm carViewModel = _carToCarVmMapper.Map(car);

            return new CreatedAtRouteResult(RouteNames.GetCar, new { carId = carViewModel.CarId }, carViewModel);
        }
    }
}
