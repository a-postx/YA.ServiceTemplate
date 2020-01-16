using System;

namespace YA.ServiceTemplate.Core.Entities
{
    public class ApiRequest
    {
        private ApiRequest()
        {
            
        }

        public ApiRequest(Guid correlationId, DateTime dateTime, string method)
        {
            ApiRequestId = correlationId;
            ApiRequestDateTime = dateTime;
            Method = method;
        }

        public Guid ApiRequestId { get; private set; }
        public DateTime ApiRequestDateTime { get; private set; }
        public string Method { get; set; }
        public int? ResponseStatusCode { get; private set; }
        public string ResponseBody { get; private set; }
        public byte[] tstamp { get; set; }

        public void SetResponseStatusCode(int? statusCode)
        {
            ResponseStatusCode = statusCode;
        }

        public void SetResponseBody(string response)
        {
            ResponseBody = response;
        }
    }
}
