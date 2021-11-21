using Delobytes.AspNetCore.Application;
using Delobytes.AspNetCore.Application.Commands;
using Delobytes.Mapper;
using MediatR;
using YA.ServiceTemplate.Application.Models.SaveModels;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.Features.Cars.Commands;

public class CreateCarCommand : IRequest<ICommandResult<Car>>
{
    public CreateCarCommand(CarSm sm)
    {
        CarSm = sm;
    }

    public CarSm CarSm { get; protected set; }

    public class CreateCarHandler : IRequestHandler<CreateCarCommand, ICommandResult<Car>>
    {
        public CreateCarHandler(ILogger<CreateCarHandler> logger,
            IAppRepository carRepository,
            IMapper<CarSm, Car> carSmToCarMapper)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
            _carSmToCarMapper = carSmToCarMapper ?? throw new ArgumentNullException(nameof(carSmToCarMapper));
        }

        private readonly ILogger<CreateCarHandler> _log;
        private readonly IAppRepository _carRepository;
        private readonly IMapper<CarSm, Car> _carSmToCarMapper;

        public async Task<ICommandResult<Car>> Handle(CreateCarCommand command, CancellationToken cancellationToken)
        {
            CarSm carSm = command.CarSm;

            Car car = _carSmToCarMapper.Map(carSm);
            car = await _carRepository.AddAsync(car, cancellationToken);

            return new CommandResult<Car>(CommandStatus.Ok, car);
        }
    }
}
