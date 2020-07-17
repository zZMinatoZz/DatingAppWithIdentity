using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Helpers
{
    public class PagedList<T> : List<T>
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public PagedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            // ceiling: lam tron len
            // floor: lam tron xuong
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            this.AddRange(items);
        }

        public static async Task<PagedList<T>> CreateAsync(IQueryable<T> source,
         int pageNumber, int pageSize)
         {
             var count = await source.CountAsync();
             // pageNumber = 1, pageSize = 5 => 5
             // skip(5) skip 5 phan tu dau tien
             // take(5) lay 5 phan tu tiep theo
             var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
             return new PagedList<T>(items, count, pageNumber, pageSize);
         }
    }
}