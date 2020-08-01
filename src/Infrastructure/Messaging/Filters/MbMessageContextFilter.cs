using GreenPipes;
using System.Threading.Tasks;

namespace YA.ServiceTemplate.Infrastructure.Messaging.Filters
{
    public class MbMessageContextFilter<T> : IFilter<T> where T : class, PipeContext
    {
        public void Probe(ProbeContext context)
        {
            ProbeContext scope = context.CreateFilterScope("MbMessageContextFilter");
        }

        public async Task Send(T context, IPipe<T> next)
        {
            // можно также создать сервисный контекст и уничтожить в конце
            using (MbMessageContextStack.Push(context))
            {
                await next.Send(context);
            }
        }
    }

}
