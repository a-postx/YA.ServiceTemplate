namespace YA.ServiceTemplate.Constants;

public static class MbQueueNames
{
    internal static string PrivateServiceQueueName = "ya.servicetemplate." + Node.Id;

    public const string MessageBusPublishQueuePrefix = "servicetemplate";
}
