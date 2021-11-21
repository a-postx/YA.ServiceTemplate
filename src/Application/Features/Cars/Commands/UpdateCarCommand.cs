using Delobytes.AspNetCore.Application;
using Delobytes.AspNetCore.Application.Commands;
using Delobytes.Mapper;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.SaveModels;
using YA.ServiceTemplate.Application.Validators;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.Features.Cars.Commands;

public class UpdateCarCommand : IRequest<ICommandResult<Car>>
{
    public UpdateCarCommand(int id, JsonPatchDocument<CarSm> patch)
    {
        Id = id;
        Patch = patch;
    }

    public int Id { get; protected set; }
    public JsonPatchDocument<CarSm> Patch { get; protected set; }

    public class UpdateCarHandler : IRequestHandler<UpdateCarCommand, ICommandResult<Car>>
    {
        public UpdateCarHandler(ILogger<UpdateCarHandler> logger,
            IAppRepository carRepository,
            IMapper<Car, CarSm> carToCarSmMapper,
            IMapper<CarSm, Car> carSmToCarMapper)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
            _carToCarSmMapper = carToCarSmMapper ?? throw new ArgumentNullException(nameof(carToCarSmMapper));
            _carSmToCarMapper = carSmToCarMapper ?? throw new ArgumentNullException(nameof(carSmToCarMapper));
        }

        private readonly ILogger<UpdateCarHandler> _log;
        private readonly IAppRepository _carRepository;
        private readonly IMapper<Car, CarSm> _carToCarSmMapper;
        private readonly IMapper<CarSm, Car> _carSmToCarMapper;

        public async Task<ICommandResult<Car>> Handle(UpdateCarCommand command, CancellationToken cancellationToken)
        {
            int carId = command.Id;

            Car car = await _carRepository.GetAsync(carId, cancellationToken);

            if (car == null)
            {
                return new CommandResult<Car>(CommandStatus.NotFound, null);
            }

            CarSm carSm = _carToCarSmMapper.Map(car);

            command.Patch.ApplyTo(carSm);

            CarSmValidator validator = new CarSmValidator();
            ValidationResult validationResult = validator.Validate(carSm);

            if (!validationResult.IsValid)
            {
                return new CommandResult<Car>(CommandStatus.ModelInvalid, null, validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
            }

            _carSmToCarMapper.Map(carSm, car);

            await _carRepository.UpdateAsync(car, cancellationToken);

            return new CommandResult<Car>(CommandStatus.Ok, car);
        }
    }
}
