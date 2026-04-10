namespace LTC.Shared.CrossCuttingConcerns.Dtos.Pagination
{
    public interface IPaginationRequestDto
    {
        /// <summary>
        /// Number of items to fetch
        /// </summary>
        public int Fetch { get; set; }

        /// <summary>
        /// Number of items to skip
        /// </summary>
        public int Page { get; set; }
    }

    public interface ISearchRequestDto
    {
        public string Keyword { get; set; }
    }
}
