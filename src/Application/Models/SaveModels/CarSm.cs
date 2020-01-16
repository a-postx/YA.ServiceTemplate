using System.ComponentModel.DataAnnotations;

namespace YA.ServiceTemplate.Application.Models.SaveModels
{
    /// <summary>
    /// Car model coming from external API call.
    /// </summary>
    public class CarSm
    {
        /// <summary>
        /// Number of motor cylinders.
        /// </summary>
        [Range(1, 20)]
        public int Cylinders { get; set; }

        /// <summary>
        /// Car brand.
        /// </summary>
        [Required]
        public string Brand { get; set; }

        /// <summary>
        /// Car model.
        /// </summary>
        [Required]
        public string Model { get; set; }
    }
}
