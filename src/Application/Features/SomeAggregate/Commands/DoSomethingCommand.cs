using MediatR;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Application.Interfaces;

namespace YA.ServiceTemplate.Application.Features.SomeAggregate.Commands;

public class DoSomethingCommand : IRequest<ICommandResult<string>>
{
    public DoSomethingCommand(string thingToDo)
    {
        ThingToDo = thingToDo;
    }

    public string ThingToDo { get; protected set; }

    public class DoSomethingHandler : IRequestHandler<DoSomethingCommand, ICommandResult<string>>
    {
        public DoSomethingHandler(ILogger<DoSomethingHandler> logger)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private readonly ILogger<DoSomethingHandler> _log;

        public async Task<ICommandResult<string>> Handle(DoSomethingCommand command, CancellationToken cancellationToken)
        {
            await Task.Delay(1000, cancellationToken);

            _log.LogInformation("Thing {Thing} done", command.ThingToDo);

            return new CommandResult<string>(CommandStatus.Ok, "world!");
        }
    }
}
