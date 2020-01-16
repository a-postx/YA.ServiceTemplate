namespace YA.ServiceTemplate.Constants
{
    public class MbQueueNames
    {
        public static string PrivateServiceQueueName = "ya.servicetemplate." + Node.Id;

        public const string MessageBusPublishQueuePrefix = "servicetemplate";
    }
}
