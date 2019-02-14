namespace HCore.Database.ElasticSearch
{
    public class ElasticSearchConstants
    {
        // search will become expensive above 5000 records

        public const int MaxOffset = 5000;
        public const int MaxPagingSize = 50;
    }
}
