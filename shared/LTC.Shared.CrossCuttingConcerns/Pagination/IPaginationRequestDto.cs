namespace LTC.Shared.CrossCuttingConcerns.Pagination
{
    public interface IPaginationRequestDto
    {
        /// <summary>
        /// Page number (starting from 1)
        /// </summary>
        public int Fetch { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int Page { get; set; }
    }

    public interface ISearchRequestDto
    {
        public string Keyword { get; set; }
    }
}
