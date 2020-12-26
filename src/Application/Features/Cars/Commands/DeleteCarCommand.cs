using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.Features.Cars.Commands
{
    public class DeleteCarCommand : IRequest<ICommandResult<Empty>>
    {
        public DeleteCarCommand(int id)
        {
            Id = id;
        }

        public int Id { get; protected set; }

        public class DeleteCarHandler : IRequestHandler<DeleteCarCommand, ICommandResult<Empty>>
        {
            public DeleteCarHandler(ILogger<DeleteCarHandler> logger, IAppRepository carRepository)
            {
                _log = logger ?? throw new ArgumentNullException(nameof(logger));
                _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
            }

            private readonly ILogger<DeleteCarHandler> _log;
            private readonly IAppRepository _carRepository;

            public async Task<ICommandResult<Empty>> Handle(DeleteCarCommand command, CancellationToken cancellationToken)
            {
                int carId = command.Id;

                Car car = await _carRepository.GetAsync(carId, cancellationToken);

                if (car == null)
                {
                    return new CommandResult<Empty>(CommandStatus.NotFound, null);
                }

                await _carRepository.DeleteAsync(car, cancellationToken);
                
                return new CommandResult<Empty>(CommandStatus.Ok, null);
            }
        }
    }
}
