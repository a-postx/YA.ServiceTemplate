using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;

namespace YA.ServiceTemplate.Application.Models.ViewModels
{
    public class PaginatedResult<T> : ValueObject where T : class
    {
        public PaginatedResult(LinkGenerator linkGenerator, PageOptions pageOptions, bool hasNextPage, bool hasPreviousPage,
            int totalCount, string startCursor, string endCursor, HttpContext context, string routeName, List<T> items)
        {
            if (linkGenerator == null)
            {
                throw new ArgumentNullException(nameof(linkGenerator));
            }

            if (pageOptions == null)
            {
                throw new ArgumentNullException(nameof(pageOptions));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (routeName == null)
            {
                throw new ArgumentNullException(nameof(routeName));
            }

            Items = items ?? new List<T>();
            PageInfo = new PageInfo()
            {
                Count = items.Count,
                HasNextPage = hasNextPage,
                HasPreviousPage = hasPreviousPage,
                NextPageUrl = hasNextPage ? new Uri(linkGenerator.GetUriByRouteValues(
                                context,
                                routeName,
                                new PageOptions()
                                {
                                    First = pageOptions.First,
                                    Last = pageOptions.Last,
                                    After = endCursor,
                                })) : null,
                PreviousPageUrl = hasPreviousPage ? new Uri(linkGenerator.GetUriByRouteValues(
                                context,
                                routeName,
                                new PageOptions()
                                {
                                    First = pageOptions.First,
                                    Last = pageOptions.Last,
                                    Before = startCursor
                                })) : null,
                FirstPageUrl = new Uri(linkGenerator.GetUriByRouteValues(
                                context,
                                routeName,
                                new PageOptions()
                                {
                                    First = pageOptions.First ?? pageOptions.Last,
                                })),
                LastPageUrl = new Uri(linkGenerator.GetUriByRouteValues(
                                context,
                                routeName,
                                new PageOptions()
                                {
                                    Last = pageOptions.First ?? pageOptions.Last,
                                })),
            };
            TotalCount = totalCount;
        }

        public int TotalCount { get; private set; }

        public PageInfo PageInfo { get; private set; }

        public List<T> Items { get; private set; }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return TotalCount;
            yield return PageInfo;
            yield return Items;
        }
    }
}
