namespace YA.ServiceTemplate.Health.System
{
    /// <summary>
    /// Memory options for health checker.
    /// </summary>
    public class MemoryCheckOptions
    {
        public int ProcessMaxMemoryThreshold { get; set; } = 512;
    }
}
