using Delobytes.AspNetCore;
using YA.ServiceTemplate.Application.Models.SaveModels;

namespace YA.ServiceTemplate.Application.ActionHandlers.Cars
{
    public interface IPutCarAh : IAsyncCommand<int, CarSm>
    {

    }
}
