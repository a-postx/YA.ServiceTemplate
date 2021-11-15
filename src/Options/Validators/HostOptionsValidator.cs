using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace YA.ServiceTemplate.Options.Validators;

public class HostOptionsValidator : IValidateOptions<HostOptions>
{
    public ValidateOptionsResult Validate(string name, HostOptions options)
    {
        List<string> failures = new List<string>();

        // время ожидания, пока все фоновые сервисы остановятся
        if (options.ShutdownTimeout < TimeSpan.FromSeconds(15))
        {
            failures.Add("Host shutdown timeout is lower than 15 seconds which may affect graceful shutdown for long-running processes.");
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
