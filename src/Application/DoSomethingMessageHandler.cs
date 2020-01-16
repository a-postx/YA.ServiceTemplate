using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Interfaces;

namespace YA.ServiceTemplate.Application
{
    public class DoSomethingMessageHandler : IDoSomethingMessageHandler
    {
        public DoSomethingMessageHandler(ILogger<DoSomethingMessageHandler> logger)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private readonly ILogger<DoSomethingMessageHandler> _log;

        public Task ServiceTheThingAsync(string value)
        {
            _log.LogInformation("Message bus message handled!");

            return Task.CompletedTask;
        }
    }
}
