using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.HttpQueryParams;
using YA.ServiceTemplate.Application.Models.ViewModels;

namespace YA.ServiceTemplate.Application.Services
{
    /// <summary>
	/// Фабрика модели результата постраничного вывода
	/// </summary>
    public class PaginatedResultFactory : IPaginatedResultFactory
    {
        public PaginatedResultFactory(IActionContextAccessor actionCtx, LinkGenerator linkGenerator)
        {
            _actionCtx = actionCtx ?? throw new ArgumentNullException(nameof(actionCtx));
            _linkGenerator = linkGenerator ?? throw new ArgumentNullException(nameof(linkGenerator));
        }

        private readonly IActionContextAccessor _actionCtx;
        private readonly LinkGenerator _linkGenerator;

        private const string ApiVersionQueryKey = "api-version";

        public PaginatedResultVm<T> GetPaginatedResult<T>(PageOptions pageOptions, bool hasNextPage, bool hasPreviousPage,
            int totalCount, string startCursor, string endCursor, string routeName, ICollection<T> items) where T : class
        {
            if (pageOptions == null)
            {
                throw new ArgumentNullException(nameof(pageOptions));
            }

            if (string.IsNullOrEmpty(routeName))
            {
                throw new ArgumentNullException(nameof(routeName));
            }
            
            ICollection<T> resultItems = items ?? new List<T>();

            Tuple<ExpandoObject, ExpandoObject> baseQueryParams = GetUniqueQueryParams(pageOptions);

            PageInfoVm pageInfo = new PageInfoVm()
            {
                Count = items.Count,
                HasNextPage = hasNextPage,
                HasPreviousPage = hasPreviousPage,
                NextPageUrl = hasNextPage ? GetNextPageUri(routeName, baseQueryParams, pageOptions, endCursor) : null,
                PreviousPageUrl = hasPreviousPage ? GetPreviousPageUri(routeName, baseQueryParams, pageOptions, startCursor) : null,
                FirstPageUrl = GetFirstPageUri(routeName, baseQueryParams, pageOptions),
                LastPageUrl = GetLastPageUri(routeName, baseQueryParams, pageOptions),
            };

            PaginatedResultVm<T> result = new PaginatedResultVm<T>(totalCount, pageInfo, resultItems);

            return result;
        }

        private Tuple<ExpandoObject, ExpandoObject> GetUniqueQueryParams(PageOptions pageOptions)
        {
            ExpandoObject uniqueParams = new ExpandoObject();
            ExpandoObject apiVersionParam = new ExpandoObject();

            foreach (KeyValuePair<string, StringValues> item in _actionCtx.ActionContext.HttpContext.Request.Query)
            {
                string key = item.Key.ToLowerInvariant();

                if (key != nameof(pageOptions.First).ToLowerInvariant()
                    && key != nameof(pageOptions.Last).ToLowerInvariant()
                    && key != nameof(pageOptions.Before).ToLowerInvariant()
                    && key != nameof(pageOptions.After).ToLowerInvariant())
                {
                    if (key == ApiVersionQueryKey)
                    {
                        apiVersionParam.TryAdd(key, item.Value);
                    }
                    else
                    {
                        uniqueParams.TryAdd(key, item.Value);
                    }
                }
            }

            return new Tuple<ExpandoObject, ExpandoObject>(uniqueParams, apiVersionParam);
        }

        private static ExpandoObject CopyUniqueQueryParams(ExpandoObject original)
        {
            ExpandoObject clone = new ExpandoObject();
            IDictionary<string, object> convertedClone = clone;

            foreach (KeyValuePair<string, object> kvp in original)
            {
                convertedClone.Add(kvp);
            }

            return clone;
        }

        private Uri GetNextPageUri(string routeName, Tuple<ExpandoObject, ExpandoObject> baseUrlParams, PageOptions pageOptions, string endCursor)
        {
            ExpandoObject baseParams = CopyUniqueQueryParams(baseUrlParams.Item1);

            baseParams.TryAdd(nameof(pageOptions.First), pageOptions.First ?? pageOptions.Last);
            baseParams.TryAdd(nameof(pageOptions.After), endCursor);

            IDictionary<string, object> apiKvp = baseUrlParams.Item2;
            if (apiKvp.TryGetValue(ApiVersionQueryKey, out object versionValue))
            {
                baseParams.TryAdd(ApiVersionQueryKey, versionValue);
            }

            return new Uri(_linkGenerator.GetUriByRouteValues(_actionCtx.ActionContext.HttpContext, routeName, baseParams));
        }

        private Uri GetPreviousPageUri(string routeName, Tuple<ExpandoObject, ExpandoObject> baseUrlParams, PageOptions pageOptions, string startCursor)
        {
            ExpandoObject baseParams = CopyUniqueQueryParams(baseUrlParams.Item1);

            baseParams.TryAdd(nameof(pageOptions.Last), pageOptions.First ?? pageOptions.Last);
            baseParams.TryAdd(nameof(pageOptions.Before), startCursor);

            IDictionary<string, object> apiKvp = baseUrlParams.Item2;
            if (apiKvp.TryGetValue(ApiVersionQueryKey, out object versionValue))
            {
                baseParams.TryAdd(ApiVersionQueryKey, versionValue);
            }

            return new Uri(_linkGenerator.GetUriByRouteValues(_actionCtx.ActionContext.HttpContext, routeName, baseParams));
        }

        private Uri GetFirstPageUri(string routeName, Tuple<ExpandoObject, ExpandoObject> baseUrlParams, PageOptions pageOptions)
        {
            ExpandoObject baseParams = CopyUniqueQueryParams(baseUrlParams.Item1);

            baseParams.TryAdd(nameof(pageOptions.First), pageOptions.First ?? pageOptions.Last);

            IDictionary<string, object> apiKvp = baseUrlParams.Item2;
            if (apiKvp.TryGetValue(ApiVersionQueryKey, out object versionValue))
            {
                baseParams.TryAdd(ApiVersionQueryKey, versionValue);
            }

            return new Uri(_linkGenerator.GetUriByRouteValues(_actionCtx.ActionContext.HttpContext, routeName, baseParams));
        }

        private Uri GetLastPageUri(string routeName, Tuple<ExpandoObject, ExpandoObject> baseUrlParams, PageOptions pageOptions)
        {
            ExpandoObject baseParams = CopyUniqueQueryParams(baseUrlParams.Item1);

            baseParams.TryAdd(nameof(pageOptions.Last), pageOptions.First ?? pageOptions.Last);

            IDictionary<string, object> apiKvp = baseUrlParams.Item2;
            if (apiKvp.TryGetValue(ApiVersionQueryKey, out object versionValue))
            {
                baseParams.TryAdd(ApiVersionQueryKey, versionValue);
            }

            return new Uri(_linkGenerator.GetUriByRouteValues(_actionCtx.ActionContext.HttpContext, routeName, baseParams));
        }
    }
}
