using GreenPipes;
using System.Collections.Generic;

namespace YA.ServiceTemplate.Infrastructure.Messaging.Filters
{
    public class MbMessageContextFilterPipeSpecification<T> : IPipeSpecification<T> where T : class, PipeContext
    {
        public void Apply(IPipeBuilder<T> builder)
        {
            var filter = new MbMessageContextFilter<T>();

            builder.AddFilter(filter);
        }

        public IEnumerable<ValidationResult> Validate()
        {
            yield break;
        }
    }

}
