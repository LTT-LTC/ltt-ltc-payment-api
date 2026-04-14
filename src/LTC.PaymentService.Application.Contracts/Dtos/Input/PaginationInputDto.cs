using Volo.Abp.Application.Dtos;

namespace LTC.PaymentService.Dtos.Input;

public class PaginationInputDto : PagedAndSortedResultRequestDto
{
    public int Fetch { get; set; } = 10;
    public int Page { get; set; } = 1;
    public string OrderBy { get; set; } = "Id";
    public bool IsSortDesc { get; set; } = true;
    public string? Keyword { get; set; }

    public override int MaxResultCount { get => Fetch; set => Fetch = value; }
    public override int SkipCount { get => (Page - 1) * Fetch; set { } }
    public override string Sorting { get => $"{OrderBy} {(IsSortDesc ? "DESC" : "ASC")}".Trim(); set { } }
}
