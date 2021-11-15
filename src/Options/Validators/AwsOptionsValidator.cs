using Microsoft.Extensions.Options;

namespace YA.ServiceTemplate.Options.Validators;

public class AwsOptionsValidator : IValidateOptions<AwsOptions>
{
    public ValidateOptionsResult Validate(string name, AwsOptions options)
    {
        List<string> failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Region))
        {
            failures.Add($"{nameof(options.Region)} option is not found.");
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
