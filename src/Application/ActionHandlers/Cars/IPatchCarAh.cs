using Delobytes.AspNetCore;
using Microsoft.AspNetCore.JsonPatch;
using YA.ServiceTemplate.Application.Models.SaveModels;

namespace YA.ServiceTemplate.Application.ActionHandlers.Cars;

public interface IPatchCarAh : IAsyncCommand<int, JsonPatchDocument<CarSm>>
{

}
