
using FluentValidation.Results;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Application.Interfaces;

namespace YA.ServiceTemplate.Application.Features
{
    public class CommandResult<TResult> : ICommandResult<TResult>
    {
        private CommandResult() { }

        public CommandResult(CommandStatuses status, TResult data, ValidationResult validationResult = null)
        {
            Status = status;
            Data = data;
            ValidationResult = validationResult;
        }

        public CommandStatuses Status { get; protected set; }
        public TResult Data { get; protected set; }
        public ValidationResult ValidationResult { get; protected set; }
    }
}
