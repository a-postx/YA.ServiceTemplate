using System;

namespace YA.ServiceTemplate.Application.Models.ViewModels
{
    /// <summary>
    /// Car view model.
    /// </summary>
    public class CarVm
    {
        /// <summary>
        /// Car unique identifier.
        /// </summary>
        public int CarId { get; set; }

        /// <summary>
        /// Number of motor cylinders.
        /// </summary>
        public int Cylinders { get; set; }

        /// <summary>
        /// Car brand.
        /// </summary>
        public string Brand { get; set; }
        
        /// <summary>
        /// Car model.
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// URL used to retrieve the resource conforming to REST'ful JSON http://restfuljson.org/.
        /// </summary>
        public Uri Url { get; set; }
    }
}
