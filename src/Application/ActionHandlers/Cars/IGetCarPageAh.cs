using Delobytes.AspNetCore;
using YA.ServiceTemplate.Application.Models.Dto;

namespace YA.ServiceTemplate.Application.ActionHandlers.Cars
{
    public interface IGetCarPageAh : IAsyncCommand<PageOptions>
    {

    }
}
