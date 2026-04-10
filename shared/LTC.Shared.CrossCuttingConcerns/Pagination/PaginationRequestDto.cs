using System.ComponentModel.DataAnnotations;

namespace LTC.Shared.CrossCuttingConcerns.Pagination
{
    public class PaginationRequestDto : IPaginationRequestDto
    {
        /// <summary>
        /// Number of items per page (default is 10, maximum is 200)
        /// </summary>
        [Range(1, 200)]
        public virtual int Fetch { get; set; } = 10;

        /// <summary>
        /// Current page number (starting from 1)
        /// </summary>
        [Range(1, int.MaxValue)]
        public virtual int Page { get; set; } = 1;
    }

    public class SearchRequestDto : ISearchRequestDto
    {
        [MaxLength(256)]
        public string? Keyword { get; set; }
    }

    public class PaginationWithSearchRequestDto : PaginationRequestDto, ISearchRequestDto
    {
        [MaxLength(256)]
        public virtual string? Keyword { get; set; }
    }
}
