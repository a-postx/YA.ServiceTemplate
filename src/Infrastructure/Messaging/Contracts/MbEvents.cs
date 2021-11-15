using MassTransit;

namespace MbEvents;

public interface ISomethingHappenedV1 : CorrelatedBy<Guid>
{
    string Value { get; }
}

public interface ISomethingHappenedHandledV1 : CorrelatedBy<Guid>
{
    string Value { get; }
}
