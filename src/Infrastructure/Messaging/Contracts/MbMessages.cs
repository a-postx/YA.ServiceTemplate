using System;
using MassTransit;

namespace MbMessages
{
    public interface IDoSomethingMessageV1 : CorrelatedBy<Guid>
    {
        string Value { get; }
    }

    public interface ISomethingDoneMessageV1 : CorrelatedBy<Guid>
    {
        string Value { get; }
    }
}
