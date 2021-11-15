namespace YA.ServiceTemplate.Application.Interfaces;

public interface IMessageBus
{
    Task SomethingHappenedHandledV1Async(string value, CancellationToken cancellationToken);
}
