using Microsoft.AspNetCore.JsonPatch;
using Delobytes.AspNetCore;
using YA.ServiceTemplate.Application.Models.SaveModels;

namespace YA.ServiceTemplate.Application.Commands
{
    public interface IPatchCarCommand : IAsyncCommand<int, JsonPatchDocument<CarSm>>
    {

    }
}
