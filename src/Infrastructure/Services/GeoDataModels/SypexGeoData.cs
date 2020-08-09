namespace YA.ServiceTemplate.Infrastructure.Services.GeoDataModels
{
    public class SypexGeoData
    {
        public string ip { get; set; }
        public SypexCity city { get; set; }
        public SypexRegion region { get; set; }
        public SypexCountry country { get; set; }
        public string error { get; set; }
        public int request { get; set; }
        public string created { get; set; }
        public int timestamp { get; set; }
    }
}
