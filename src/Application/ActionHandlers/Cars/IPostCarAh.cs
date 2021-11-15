using Delobytes.AspNetCore;
using YA.ServiceTemplate.Application.Models.SaveModels;

namespace YA.ServiceTemplate.Application.ActionHandlers.Cars;

public interface IPostCarAh : IAsyncCommand<CarSm>
{

}
