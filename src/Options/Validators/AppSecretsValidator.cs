using Microsoft.Extensions.Options;

namespace YA.ServiceTemplate.Options.Validators;

public class AppSecretsValidator : IValidateOptions<AppSecrets>
{
    public ValidateOptionsResult Validate(string name, AppSecrets options)
    {
        List<string> failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.MessageBusHost))
        {
            failures.Add($"{nameof(options.MessageBusHost)} secret is not found.");
        }

        if (options.MessageBusPort <= 0)
        {
            failures.Add($"{nameof(options.MessageBusPort)} secret is not found.");
        }

        if (string.IsNullOrWhiteSpace(options.MessageBusVHost))
        {
            failures.Add($"{nameof(options.MessageBusVHost)} secret is not found.");
        }

        if (string.IsNullOrWhiteSpace(options.MessageBusLogin))
        {
            failures.Add($"{nameof(options.MessageBusLogin)} secret is not found.");
        }

        if (string.IsNullOrWhiteSpace(options.MessageBusPassword))
        {
            failures.Add($"{nameof(options.MessageBusPassword)} secret is not found.");
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
