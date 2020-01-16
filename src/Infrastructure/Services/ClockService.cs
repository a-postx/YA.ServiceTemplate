using System;
using YA.ServiceTemplate.Application.Interfaces;

namespace YA.ServiceTemplate.Infrastructure.Services
{
    /// <summary>
    /// Retrieves the current date and/or time. Helps with unit testing by letting you mock the system clock.
    /// </summary>
    public class ClockService : IClockService
    {
        /// <summary>
        /// Current time in UTC.
        /// </summary>
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
