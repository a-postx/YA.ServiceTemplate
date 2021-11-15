using Delobytes.AspNetCore;
using YA.ServiceTemplate.Application.Models.HttpQueryParams;

namespace YA.ServiceTemplate.Application.ActionHandlers.Cars;

public interface IGetCarPageAh : IAsyncCommand<PageOptionsCursor>
{

}
