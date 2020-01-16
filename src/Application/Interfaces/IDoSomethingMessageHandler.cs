using System.Threading.Tasks;

namespace YA.ServiceTemplate.Application.Interfaces
{
    public interface IDoSomethingMessageHandler
    {
        Task ServiceTheThingAsync(string value);
    }
}
