using Microsoft.Extensions.Options;

namespace YA.ServiceTemplate.Options.Validators;

public class GeneralOptionsValidator : IValidateOptions<GeneralOptions>
{
    public ValidateOptionsResult Validate(string name, GeneralOptions options)
    {
        List<string> failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.CorrelationIdHeader))
        {
            failures.Add($"{nameof(options.CorrelationIdHeader)} option is not found.");
        }

        if (options.MaxLogFieldLength <= 0)
        {
            failures.Add($"{nameof(options.MaxLogFieldLength)} option is not found.");
        }

        if (options.DefaultPaginationPageSize <= 0)
        {
            failures.Add($"{nameof(options.DefaultPaginationPageSize)} option is not found.");
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
