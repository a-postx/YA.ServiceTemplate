using Delobytes.Mapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.SaveModels;
using YA.ServiceTemplate.Application.Models.ViewModels;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.Commands
{
    public class PatchCarCommand : IPatchCarCommand
    {
        public PatchCarCommand(
            ILogger<PatchCarCommand> logger,
            IActionContextAccessor actionContextAccessor,
            IObjectModelValidator objectModelValidator,
            IAppRepository carRepository,
            IMapper<Car, CarVm> carToCarVmMapper,
            IMapper<Car, CarSm> carToCarSmMapper,
            IMapper<CarSm, Car> carSmToCarMapper)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
            _actionContextAccessor = actionContextAccessor ?? throw new ArgumentNullException(nameof(actionContextAccessor));
            _objectModelValidator = objectModelValidator ?? throw new ArgumentNullException(nameof(objectModelValidator));
            _carToCarVmMapper = carToCarVmMapper ?? throw new ArgumentNullException(nameof(carToCarVmMapper));
            _carToCarSmMapper = carToCarSmMapper ?? throw new ArgumentNullException(nameof(carToCarSmMapper));
            _carSmToCarMapper = carSmToCarMapper ?? throw new ArgumentNullException(nameof(carSmToCarMapper));
        }

        private readonly ILogger<PatchCarCommand> _log;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IObjectModelValidator _objectModelValidator;
        private readonly IAppRepository _carRepository;
        private readonly IMapper<Car, CarVm> _carToCarVmMapper;
        private readonly IMapper<Car, CarSm> _carToCarSmMapper;
        private readonly IMapper<CarSm, Car> _carSmToCarMapper;

        public async Task<IActionResult> ExecuteAsync(
            int carId,
            JsonPatchDocument<CarSm> patch,
            CancellationToken cancellationToken)
        {
            Car car = await _carRepository.GetAsync(carId, cancellationToken);

            if (car == null)
            {
                return new NotFoundResult();
            }

            CarSm saveCar = _carToCarSmMapper.Map(car);
            ModelStateDictionary modelState = _actionContextAccessor.ActionContext.ModelState;
            patch.ApplyTo(saveCar, modelState);

            _objectModelValidator.Validate(_actionContextAccessor.ActionContext, null, null, saveCar);

            if (!modelState.IsValid)
            {
                return new BadRequestObjectResult(modelState);
            }

            _carSmToCarMapper.Map(saveCar, car);
            await _carRepository.UpdateAsync(car, cancellationToken);
            CarVm carViewModel = _carToCarVmMapper.Map(car);

            return new OkObjectResult(carViewModel);
        }
    }
}