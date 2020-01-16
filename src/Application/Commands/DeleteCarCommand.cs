using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.Commands
{
    public class DeleteCarCommand : IDeleteCarCommand
    {
        public DeleteCarCommand(ILogger<DeleteCarCommand> logger, IAppRepository carRepository)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
        }

        private readonly ILogger<DeleteCarCommand> _log;
        private readonly IAppRepository _carRepository;

        public async Task<IActionResult> ExecuteAsync(int carId, CancellationToken cancellationToken)
        {
            Car car = await _carRepository.GetAsync(carId, cancellationToken);

            if (car == null)
            {
                return new NotFoundResult();
            }

            await _carRepository.DeleteAsync(car, cancellationToken);

            return new NoContentResult();
        }
    }
}
