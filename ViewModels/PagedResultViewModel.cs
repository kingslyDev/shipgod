namespace ShipmentFinishGood.ViewModels;

public class PagedResultViewModel<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
    public string? Search { get; set; }
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}
