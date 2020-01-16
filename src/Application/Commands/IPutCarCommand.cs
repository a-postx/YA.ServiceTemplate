using Delobytes.AspNetCore;
using YA.ServiceTemplate.Application.Models.SaveModels;

namespace YA.ServiceTemplate.Application.Commands
{
    public interface IPutCarCommand : IAsyncCommand<int, CarSm>
    {

    }
}
