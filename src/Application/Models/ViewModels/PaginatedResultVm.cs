using System;
using System.Collections.Generic;

namespace YA.ServiceTemplate.Application.Models.ViewModels
{
    /// <summary>
    /// Постраничный результат вывода элементов общего типа.
    /// </summary>
    /// <typeparam name="T">Тип выводимого элемента.</typeparam>
    public class PaginatedResultVm<T> : ValueObject where T : class
    {
        public PaginatedResultVm(int totalCount, PageInfoVm pageInfo, ICollection<T> items)
        {
            TotalCount = totalCount;
            PageInfo = pageInfo ?? throw new ArgumentNullException(nameof(pageInfo));
            Items = items ?? throw new ArgumentNullException(nameof(items));
        }

        /// <summary>
        /// Общее количество элементов.
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// Модель страницы.
        /// </summary>
        public PageInfoVm PageInfo { get; private set; }

        /// <summary>
        /// Список элементов.
        /// </summary>
        public ICollection<T> Items { get; private set; }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return TotalCount;
            yield return PageInfo;

            foreach (T item in Items)
            {
                yield return item;
            }
        }
    }
}
