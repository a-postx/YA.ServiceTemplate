using GreenPipes;
using System;

namespace YA.ServiceTemplate.Infrastructure.Messaging.Filters
{
    /// <summary>
    /// Manages a Logical Call Context variable containing a stack of <see cref="PipeContext"/> instances.
    /// </summary>
    internal static class MbMessageContextStack
    {
        /// <summary>
        /// Publishes a <see cref="PipeContext"/> onto the stack.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static IDisposable Push(PipeContext context)
        {
            return MbMessageAsyncLocalStack<PipeContext>.Push(context);
        }

        /// <summary>
        /// Gets the current <see cref="PipeContext"/>.
        /// </summary>
        public static PipeContext Current => MbMessageAsyncLocalStack<PipeContext>.Current;
    }
}
