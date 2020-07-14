using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Delobytes.Mapper;
using YA.ServiceTemplate.Application.Models.ViewModels;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.Commands
{
    public class GetCarCommand : IGetCarCommand
    {
        public GetCarCommand(ILogger<GetCarCommand> logger, IAppRepository carRepository, IActionContextAccessor actionContextAccessor, IMapper<Car, CarVm> carMapper)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
            _actionContextAccessor = actionContextAccessor ?? throw new ArgumentNullException(nameof(actionContextAccessor));
            _carMapper = carMapper ?? throw new ArgumentNullException(nameof(carMapper));
        }

        private readonly ILogger<GetCarCommand> _log;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IAppRepository _carRepository;
        private readonly IMapper<Car, CarVm> _carMapper;

        public async Task<IActionResult> ExecuteAsync(int carId, CancellationToken cancellationToken)
        {
            Car car = await _carRepository.GetAsync(carId, cancellationToken);

            if (car == null)
            {
                return new NotFoundResult();
            }

            HttpContext httpContext = _actionContextAccessor.ActionContext.HttpContext;
            if (httpContext.Request.Headers.TryGetValue(HeaderNames.IfModifiedSince, out StringValues stringValues))
            {
                if (DateTimeOffset.TryParse(stringValues, out DateTimeOffset modifiedSince) && (modifiedSince >= car.Modified))
                {
                    return new StatusCodeResult(StatusCodes.Status304NotModified);
                }
            }

            CarVm carViewModel = _carMapper.Map(car);
            httpContext.Response.Headers.Add(HeaderNames.LastModified, car.Modified.ToString("R"));

            return new OkObjectResult(carViewModel);
        }
    }
}
