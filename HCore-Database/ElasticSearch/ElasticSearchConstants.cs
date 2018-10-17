namespace HCore.Database.ElasticSearch
{
    public class ElasticSearchConstants
    {
        // search will become expensive above 500 records

        public const int MaxOffset = 500;
        public const int MaxPagingSize = 50;
    }
}
