namespace YA.ServiceTemplate.Infrastructure.Services.GeoDataModels
{
    public class SypexCountry
    {
        public int id { get; set; }
        public string iso { get; set; }
        public string continent { get; set; }
        public int lat { get; set; }
        public int lon { get; set; }
        public string name_ru { get; set; }
        public string name_en { get; set; }
        public string name_de { get; set; }
        public string name_fr { get; set; }
        public string name_it { get; set; }
        public string name_es { get; set; }
        public string name_pt { get; set; }
        public string timezone { get; set; }
        public int area { get; set; }
        public int population { get; set; }
        public int capital_id { get; set; }
        public string capital_ru { get; set; }
        public string capital_en { get; set; }
        public string cur_code { get; set; }
        public string phone { get; set; }
        public string neighbours { get; set; }
        public int vk { get; set; }
        public int utc { get; set; }
    }
}
