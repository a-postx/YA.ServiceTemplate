using Microsoft.Extensions.Options;

namespace YA.ServiceTemplate.Options.Validators;

public class IdempotencyControlOptionsValidator : IValidateOptions<IdempotencyOptions>
{
    public ValidateOptionsResult Validate(string name, IdempotencyOptions options)
    {
        List<string> failures = new List<string>();

        if (!options.IdempotencyFilterEnabled.HasValue)
        {
            failures.Add($"{nameof(options.IdempotencyFilterEnabled)} option is not found.");
        }

        if (string.IsNullOrWhiteSpace(options.IdempotencyHeader))
        {
            failures.Add($"{nameof(options.IdempotencyHeader)} option is not found.");
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }
        else
        {
            return ValidateOptionsResult.Success;
        }
    }
}
