﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.Dto;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application
{
    /// <summary>
    /// Отслеживает АПИ-запросы. Нужно подумать о смене хранилища
    /// на что-то с хорошей скоростью записи (Редис, Монго и т.п.) или оставить кеширование в памяти
    /// </summary>
    public class ApiRequestTracker : IApiRequestTracker
    {
        public ApiRequestTracker(ILogger<ApiRequestTracker> logger, IApiRequestMemoryCache apiRequestCache, IAppRepository apiRequestRepository)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _apiRequestCache = apiRequestCache ?? throw new ArgumentNullException(nameof(apiRequestCache));
            _apiRequestRepository = apiRequestRepository ?? throw new ArgumentNullException(nameof(apiRequestRepository));
        }

        private readonly ILogger<ApiRequestTracker> _log;
        private readonly IApiRequestMemoryCache _apiRequestCache;
        private readonly IAppRepository _apiRequestRepository;
        
        public async Task<(bool created, ApiRequest request)> GetOrCreateRequestAsync(Guid correlationId, string method, CancellationToken cancellationToken)
        {
            (bool requestFoundInCache, ApiRequest request) = await GetFromCacheOrDbAsync(correlationId, cancellationToken);

            if (requestFoundInCache)
            {
                return (false, request);
            }
            else
            {
                if (request != null)
                {
                    _apiRequestCache.Add(request, request.ApiRequestId);
                    return (false, request);
                }
                else
                {
                    ApiRequest newApiRequest = new ApiRequest(correlationId, DateTime.UtcNow, method);

                    ApiRequest createdRequest = await _apiRequestRepository.CreateApiRequestAsync(newApiRequest);

                    _apiRequestCache.Add(newApiRequest, newApiRequest.ApiRequestId);

                    return (true, createdRequest);
                }
            }
        }

        private async Task<(bool requestFoundInCache, ApiRequest request)> GetFromCacheOrDbAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            ApiRequest requestFromCache = _apiRequestCache.GetApiRequestFromCache<ApiRequest>(correlationId);
                
            if (requestFromCache == null)
            {
                ApiRequest request = await _apiRequestRepository.GetApiRequestAsync(correlationId);

                return (request != null) ? (false, request) : (false, null);
            }
            else
            {
                return (true, requestFromCache);
            }
        }

        public async Task SetResultAsync(ApiRequest request, ApiRequestResult result, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new Exception("Api request cannot be empty.");
            }

            if (result == null)
            {
                throw new Exception("Api request result cannot be empty.");
            }

            request.SetResponseStatusCode(result.StatusCode);
            request.SetResponseBody((result.Body != null) ? JToken.Parse(JsonConvert.SerializeObject(result.Body)).ToString(Formatting.Indented) : null);
            
            await _apiRequestRepository.UpdateApiRequestAsync(request);

            _apiRequestCache.Update(request, request.ApiRequestId);
        }
    }
}
