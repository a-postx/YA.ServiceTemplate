using Delobytes.AspNetCore;
using YA.ServiceTemplate.Application.Models.ViewModels;

namespace YA.ServiceTemplate.Application.Commands
{
    public interface IGetCarPageCommand : IAsyncCommand<PageOptions>
    {

    }
}
