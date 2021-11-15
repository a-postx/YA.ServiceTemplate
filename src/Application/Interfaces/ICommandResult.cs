using FluentValidation.Results;
using YA.ServiceTemplate.Application.Enums;

namespace YA.ServiceTemplate.Application.Interfaces;

public interface ICommandResult<TResult>
{
    public CommandStatus Status { get; }
    public TResult Data { get; }
    public ValidationResult ValidationResult { get; }
}
