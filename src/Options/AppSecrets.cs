namespace YA.ServiceTemplate.Options;

/// <summary>
/// Секреты приложения
/// </summary>
public class AppSecrets
{
    public string LogzioToken { get; set; }
    public string AppInsightsInstrumentationKey { get; set; }
    public string MessageBusHost { get; set; }
    public int MessageBusPort { get; set; }
    public string MessageBusVHost { get; set; }
    public string MessageBusLogin { get; set; }
    public string MessageBusPassword { get; set; }
}
