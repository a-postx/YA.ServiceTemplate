namespace YA.ServiceTemplate.Application.Models.Dto
{
    public class ApiRequestResult
    {
        private ApiRequestResult() { }
        public ApiRequestResult(int? code, string body)
        {
            StatusCode = code;
            Body = body;
        }

        public int? StatusCode { get; private set; }
        public string Body { get; private set; }
    }
}
