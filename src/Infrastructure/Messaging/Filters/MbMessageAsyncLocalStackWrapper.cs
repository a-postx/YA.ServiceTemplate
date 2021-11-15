using System.Collections.Immutable;

namespace YA.ServiceTemplate.Infrastructure.Messaging.Filters;

/// <summary>
/// Manages a Logical Call Context variable containing a stack of <typeparamref name="T"/> instances.
/// </summary>
public static class MbMessageAsyncLocalStackWrapper<T>
{
    /// <summary>
    /// Wraps the stack information.
    /// </summary>
    private class LogicalContextData
    {
        public ImmutableStack<T> Stack { get; set; }
    }

    private static readonly AsyncLocal<LogicalContextData> data = new AsyncLocal<LogicalContextData>();

    /// <summary>
    /// Gets the current context stack.
    /// </summary>
    private static ImmutableStack<T> CurrentContext
    {
        get => data.Value?.Stack ?? ImmutableStack<T>.Empty;
        set => data.Value = new LogicalContextData { Stack = value };
    }

    /// <summary>
    /// Publishes a <see><cref>T</cref></see> onto the stack.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static IDisposable Push(T context)
    {
        CurrentContext = CurrentContext.Push(context);
        return new PopWhenDisposed();
    }

    /// <summary>
    /// Gets the current <see><cref>T</cref></see>.
    /// </summary>
    public static T Current => Peek();

    /// <summary>
    /// Removes the last item from the stack.
    /// </summary>
    private static void Pop()
    {
        ImmutableStack<T> currentContext = CurrentContext;
        if (currentContext.IsEmpty == false)
        {
            CurrentContext = currentContext.Pop();
        }
    }

    /// <summary>
    /// Returns the last item on the stack.
    /// </summary>
    /// <returns></returns>
    private static T Peek()
    {
        ImmutableStack<T> currentContext = CurrentContext;
        if (currentContext.IsEmpty == false)
        {
            return currentContext.Peek();
        }
        else
        {
            return default;
        }
    }

    /// <summary>
    /// Implements a Pop operation when disposed.
    /// </summary>
    private class PopWhenDisposed : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Disposes of the instance.
        /// </summary>
        public void Dispose()
        {
            if (_disposed == false)
            {
                Pop();
                _disposed = true;
            }
        }
    }
}
