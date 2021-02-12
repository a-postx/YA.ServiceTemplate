using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace YA.ServiceTemplate.Options.Validators
{
    public class IdempotencyControlOptionsValidator : IValidateOptions<IdempotencyControlOptions>
    {
        public ValidateOptionsResult Validate(string name, IdempotencyControlOptions options)
        {
            List<string> failures = new List<string>();

            if (!options.IdempotencyFilterEnabled.HasValue)
            {
                failures.Add($"{nameof(options.IdempotencyFilterEnabled)} option is not found.");
            }

            if (string.IsNullOrWhiteSpace(options.ClientRequestIdHeader))
            {
                failures.Add($"{nameof(options.ClientRequestIdHeader)} option is not found.");
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
}
