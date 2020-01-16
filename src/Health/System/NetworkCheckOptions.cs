namespace YA.ServiceTemplate.Health.System
{
    /// <summary>
    /// Network options for health checker.
    /// </summary>
    public class NetworkCheckOptions
    {
        public int MaxLatencyThreshold { get; set; } = 500;
        public string InternetHost { get; set; } = "77.88.8.8";
    }
}
