using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace YA.ServiceTemplate.Options
{
    /// <summary>
    /// All options for the application.
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
    }
}
