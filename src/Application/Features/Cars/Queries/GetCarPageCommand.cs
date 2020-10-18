using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.Dto;
using YA.ServiceTemplate.Core;
using YA.ServiceTemplate.Core.Entities;
using YA.ServiceTemplate.Options;

namespace YA.ServiceTemplate.Application.Features.Cars.Queries
{
    public class GetCarPageCommand : IRequest<ICommandResult<PaginatedResult<Car>>>
    {
        public GetCarPageCommand(PageOptions pageOptions, DateTimeOffset? createdAfter, DateTimeOffset? createdBefore)
        {
            Options = pageOptions;
            CreatedAfter = createdAfter;
            CreatedBefore = createdBefore;
        }

        public PageOptions Options { get; protected set; }
        public DateTimeOffset? CreatedAfter { get; protected set; }
        public DateTimeOffset? CreatedBefore { get; protected set; }

        public class GetCarPageHandler : IRequestHandler<GetCarPageCommand, ICommandResult<PaginatedResult<Car>>>
        {
            public GetCarPageHandler(ILogger<GetCarPageHandler> logger,
                IAppRepository carRepository,
                IOptionsSnapshot<GeneralOptions> options)
            {
                _log = logger ?? throw new ArgumentNullException(nameof(logger));
                _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
                _generalOptions = options.Value;
            }

            private readonly ILogger<GetCarPageHandler> _log;
            private readonly IAppRepository _carRepository;
            private readonly GeneralOptions _generalOptions;

            public async Task<ICommandResult<PaginatedResult<Car>>> Handle(GetCarPageCommand command, CancellationToken cancellationToken)
            {
                PageOptions pageOptions = command.Options;

                if (pageOptions == null)
                {
                    return new CommandResult<PaginatedResult<Car>>(CommandStatuses.BadRequest, null);
                }

                pageOptions.First = !pageOptions.First.HasValue && !pageOptions.Last.HasValue ? _generalOptions.DefaultPaginationPageSize : pageOptions.First;

                Task<List<Car>> getCarsTask = GetCarsAsync(pageOptions.First, pageOptions.Last, command.CreatedAfter, command.CreatedBefore, cancellationToken);
                Task<bool> getHasNextPageTask = GetHasNextPageAsync(pageOptions.First, command.CreatedAfter, command.CreatedBefore, cancellationToken);
                Task<bool> getHasPreviousPageTask = GetHasPreviousPageAsync(pageOptions.Last, command.CreatedAfter, command.CreatedBefore, cancellationToken);
                Task<int> totalCountTask = _carRepository.GetTotalCountAsync(cancellationToken);

                await Task.WhenAll(getCarsTask, getHasNextPageTask, getHasPreviousPageTask, totalCountTask).ConfigureAwait(false);
                
                List<Car> cars = await getCarsTask.ConfigureAwait(false);
                bool hasNextPage = await getHasNextPageTask.ConfigureAwait(false);
                bool hasPreviousPage = await getHasPreviousPageTask.ConfigureAwait(false);
                int totalCount = await totalCountTask.ConfigureAwait(false);

                if (cars == null)
                {
                    return new CommandResult<PaginatedResult<Car>>(CommandStatuses.NotFound, null);
                }

                PaginatedResult<Car> result = new PaginatedResult<Car>(
                    hasNextPage,
                    hasPreviousPage,
                    totalCount,
                    cars
                );

                return new CommandResult<PaginatedResult<Car>>(CommandStatuses.Ok, result);
            }

            private Task<List<Car>> GetCarsAsync(
                int? first,
                int? last,
                DateTimeOffset? createdAfter,
                DateTimeOffset? createdBefore,
                CancellationToken cancellationToken)
            {
                Task<List<Car>> getCarsTask = first.HasValue
                    ? _carRepository.GetCarsAsync(first, createdAfter, createdBefore, cancellationToken)
                    : _carRepository.GetCarsReverseAsync(last, createdAfter, createdBefore, cancellationToken);

                return getCarsTask;
            }

            private async Task<bool> GetHasNextPageAsync(
                int? first,
                DateTimeOffset? createdAfter,
                DateTimeOffset? createdBefore,
                CancellationToken cancellationToken)
            {
                if (first.HasValue)
                {
                    return await _carRepository.GetHasNextPageAsync(first, createdAfter, cancellationToken).ConfigureAwait(false);
                }
                else if (createdBefore.HasValue)
                {
                    return true;
                }

                return false;
            }

            private async Task<bool> GetHasPreviousPageAsync(
                int? last,
                DateTimeOffset? createdAfter,
                DateTimeOffset? createdBefore,
                CancellationToken cancellationToken)
            {
                if (last.HasValue)
                {
                    return await _carRepository.GetHasPreviousPagAsync(last, createdBefore, cancellationToken).ConfigureAwait(false);
                }
                else if (createdAfter.HasValue)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
