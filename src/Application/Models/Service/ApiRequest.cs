using System;
using System.Collections.Generic;
using YA.ServiceTemplate.Application.Interfaces;

namespace YA.ServiceTemplate.Application.Models.Service
{
    /// <summary>
    /// Модель АПИ-запроса и результата.
    /// </summary>
    internal class ApiRequest : ICacheable
    {
        private ApiRequest() { }

        internal ApiRequest(Guid clientRequestId)
        {
            if (clientRequestId == Guid.Empty)
            {
                throw new ArgumentException(nameof(clientRequestId));
            }

            ApiRequestID = clientRequestId;

            CacheKey = $"idempotency_keys:{clientRequestId}";
            AbsoluteExpiration = new TimeSpan(24, 0, 0);
        }

        internal Guid ApiRequestID { get; private set; }
        internal string Method { get; private set; }
        internal string Path { get; private set; }
        internal string Query { get; private set; }
        internal int? StatusCode { get; private set; }
        internal Dictionary<string, List<string>> Headers { get; private set; }
        internal string Body { get; private set; }

        internal string ResultType { get; private set; }
        internal object ResultValue { get; private set; }
        internal string ResultRouteName { get; private set; }
        internal Dictionary<string, string> ResultRouteValues { get; private set; }

        public string CacheKey { get; }
        public TimeSpan AbsoluteExpiration { get; }

        internal void SetMethod(string method)
        {
            Method = method;
        }

        internal void SetPath(string path)
        {
            Path = path;
        }

        internal void SetQuery(string query)
        {
            Query = query;
        }

        internal void SetStatusCode(int? statusCode)
        {
            StatusCode = statusCode;
        }

        internal void SetHeaders(Dictionary<string, List<string>> headers)
        {
            Headers = headers;
        }

        internal void SetBody(string body)
        {
            Body = body;
        }

        internal void SetResultType(string resultType)
        {
            ResultType = resultType;
        }

        internal void SetResultValue(object resultValue)
        {
            ResultValue = resultValue;
        }

        internal void SetResultRouteName(string resultRouteName)
        {
            ResultRouteName = resultRouteName;
        }

        internal void SetResultRouteValues(Dictionary<string, string> resultRouteValues)
        {
            ResultRouteValues = resultRouteValues;
        }
    }
}
