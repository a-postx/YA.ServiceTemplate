using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;

namespace YA.ServiceTemplate.Options
{
    /// <summary>
    /// Настройки приложения
    /// </summary>
    public class ApplicationOptions
    {
        public ApplicationOptions()
        {
            CacheProfiles = new CacheProfileOptions();
        }

        [Required]
        public CacheProfileOptions CacheProfiles { get; set; }

        [Required]
        public CompressionOptions Compression { get; set; }

        [Required]
        public ForwardedHeadersOptions ForwardedHeaders { get; set; }

        [Required]
        public KestrelServerOptions Kestrel { get; set; }

        [Required]
        public HostOptions HostOptions { get; set; }

        // имя соответствует https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/net-dg-config-netcore.html
        [Required]
        public AwsOptions Aws { get; set; }

        [Required]
        public GeneralOptions General { get; set; }
    }
}
