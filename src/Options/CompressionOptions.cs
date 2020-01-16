using System.Collections.Generic;

namespace YA.ServiceTemplate.Options
{
    /// <summary>
    /// The dynamic response compression options for the application.
    /// </summary>
    public class CompressionOptions
    {
        public CompressionOptions()
        {
            MimeTypes = new List<string>();
        }
        /// <summary>
        /// Gets or sets a list of MIME types to be compressed in addition to the default set used by ASP.NET Core.
        /// </summary>
        public List<string> MimeTypes { get; set; }
    }
}
