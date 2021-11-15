using Delobytes.Mapper;
using MediatR;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.SaveModels;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.Features.Cars.Commands;

public class ReplaceCarCommand : IRequest<ICommandResult<Car>>
{
    public ReplaceCarCommand(int id, CarSm saveModel)
    {
        Id = id;
        SaveModel = saveModel;
    }

    public int Id { get; protected set; }
    public CarSm SaveModel { get; protected set; }

    public class ReplaceCarHandler : IRequestHandler<ReplaceCarCommand, ICommandResult<Car>>
    {
        public ReplaceCarHandler(ILogger<ReplaceCarHandler> logger,
            IAppRepository carRepository,
            IMapper<CarSm, Car> carSmToCarMapper)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
            _carSmToCarMapper = carSmToCarMapper ?? throw new ArgumentNullException(nameof(carSmToCarMapper));
        }

        private readonly ILogger<ReplaceCarHandler> _log;
        private readonly IAppRepository _carRepository;
        private readonly IMapper<CarSm, Car> _carSmToCarMapper;

        public async Task<ICommandResult<Car>> Handle(ReplaceCarCommand command, CancellationToken cancellationToken)
        {
            int carId = command.Id;
            CarSm carSm = command.SaveModel;

            Car car = await _carRepository.GetAsync(carId, cancellationToken);

            if (car == null)
            {
                return new CommandResult<Car>(CommandStatus.NotFound, null);
            }

            _carSmToCarMapper.Map(carSm, car);

            car = await _carRepository.UpdateAsync(car, cancellationToken);

            return new CommandResult<Car>(CommandStatus.Ok, car);
        }
    }
}
