namespace YA.ServiceTemplate.Constants
{
    public static class General
    {
        public const int HostShutdownTimeoutSec = 15;
        public const int WebHostShutdownTimeoutSec = 60;
        public const string AppDataFolderName = "AppData";
        public const string AppHttpUserAgent = "YA.ServiceTemplate/1.0";
        public const string CorrelationIdHeader = "X-Correlation-ID";
        public const string MessageBusServiceHealthCheckName = "message_bus_service";
        public const int MessageBusServiceHealthPort = 5672;
        public const int StartupServiceCheckRetryIntervalMs = 10000;
        public const int DefaultPageSizeForPagination = 3;
        public const int MaxLogFieldLength = 27716;
        public const int ApiRequestsCacheSize = 256;
        public const int ApiRequestCacheSlidingExpirationSec = 120;
        public const int ApiRequestCacheAbsoluteExpirationSec = 300;
    }
}
