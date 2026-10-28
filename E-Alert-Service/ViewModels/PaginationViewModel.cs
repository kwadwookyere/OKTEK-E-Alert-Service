using System;
using System.Collections.Generic;

namespace EAlertService.ViewModels;

public interface IPagedResult
{
    int PageNumber { get; set; }
    int PageSize { get; set; }
    int TotalRecords { get; set; }
    int TotalPages { get; }
    bool HasPreviousPage { get; }
    bool HasNextPage { get; }
    int StartRecord { get; }
    int EndRecord { get; }
}

public class PagedResult<T> : IPagedResult
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalRecords { get; set; }

    public int TotalPages => TotalRecords > 0 ? (int)Math.Ceiling((double)TotalRecords / Math.Max(PageSize, 1)) : 1;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public int StartRecord => TotalRecords == 0 ? 0 : (PageNumber - 1) * PageSize + 1;
    public int EndRecord => Math.Min(PageNumber * PageSize, TotalRecords);

    public PagedResult() { }

    public PagedResult(List<T> items, int totalRecords, int pageNumber, int pageSize)
    {
        Items = items;
        TotalRecords = totalRecords;
        PageNumber = pageNumber < 1 ? 1 : pageNumber;
        PageSize = pageSize < 1 ? 20 : pageSize;
    }
}

