namespace YA.ServiceTemplate.Application.Models.HttpQueryParams
{
    /// <summary>
    /// Page options for listing objects
    /// </summary>
    public class PageOptionsCursor
    {
        public int? First { get; set; }

        public int? Last { get; set; }

        public string After { get; set; }

        public string Before { get; set; }
    }
}
