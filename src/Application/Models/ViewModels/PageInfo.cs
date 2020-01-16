using System;
using System.Collections.Generic;

namespace YA.ServiceTemplate.Application.Models.ViewModels
{
    public class PageInfo
    {
        private const string NextLinkItem = "next";
        private const string PreviousLinkItem = "previous";
        private const string FirstLinkItem = "first";
        private const string LastLinkItem = "last";

        /// <summary>
        /// Gets or sets the count of items.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has a next page.
        /// </summary>
        public bool HasNextPage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has a previous page.
        /// </summary>
        public bool HasPreviousPage { get; set; }

        /// <summary>
        /// Gets or sets the next page URL.
        /// </summary>
        public Uri NextPageUrl { get; set; }

        /// <summary>
        /// Gets or sets the previous page URL.
        /// </summary>
        public Uri PreviousPageUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL to the first page.
        /// </summary>
        public Uri FirstPageUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL to the last page.
        /// </summary>
        public Uri LastPageUrl { get; set; }

        /// <summary>
        /// Gets the Link HTTP header value to add URL's to next, previous, first and last pages.
        /// See https://tools.ietf.org/html/rfc5988#page-6
        /// There is a standard list of link relation types e.g. next, previous, first and last.
        /// See https://www.iana.org/assignments/link-relations/link-relations.xhtml
        /// </summary>
        /// <returns>The Link HTTP header value.</returns>
        public string ToLinkHttpHeaderValue()
        {
            List<string> values = new List<string>(4);

            if (HasNextPage && NextPageUrl != null)
            {
                values.Add(GetLinkValueItem(NextLinkItem, NextPageUrl));
            }

            if (HasPreviousPage && PreviousPageUrl != null)
            {
                values.Add(GetLinkValueItem(PreviousLinkItem, PreviousPageUrl));
            }

            if (FirstPageUrl != null)
            {
                values.Add(GetLinkValueItem(FirstLinkItem, FirstPageUrl));
            }

            if (LastPageUrl != null)
            {
                values.Add(GetLinkValueItem(LastLinkItem, LastPageUrl));
            }

            return string.Join(", ", values);
        }

        private string GetLinkValueItem(string rel, Uri url) => FormattableString.Invariant($"<{url}>; rel=\"{rel}\"");
    }
}
