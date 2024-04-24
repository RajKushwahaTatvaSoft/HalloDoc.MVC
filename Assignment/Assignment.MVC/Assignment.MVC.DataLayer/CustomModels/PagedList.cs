using Microsoft.EntityFrameworkCore;

namespace Assignment.MVC.DataLayer.CustomModels
{
    public class PagedList<T> : List<T>
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool hasPrevious { get; set; } = false;
        public bool hasNext { get; set; } = false;

        public PagedList(IEnumerable<T> currentItems, int count, int pageNumber, int pageSize)
        {
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            PageSize = pageSize;
            TotalCount = count;

            if (pageNumber > 1)
            {
                hasPrevious = true;
            }

            if (pageNumber < TotalPages)
            {
                hasNext = true;
            }

            AddRange(currentItems);
        }

        public static async Task<PagedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedList<T>(items, count, pageNumber, pageSize);
        }

    }
}
