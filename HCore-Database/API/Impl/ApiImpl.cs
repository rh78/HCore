using HCore.Database.ElasticSearch;
using HCore.Database.SqlDatabase;
using HCore.Web.Exceptions;

namespace HCore.Database.API.Impl
{
    public class ApiImpl
    {
        public const int DefaultPagingOffset = 0;
        public const int DefaultPagingLimit = 10;

        public static int ProcessElasticSearchPagingOffset(int? offset, int? limit)
        {
            if (offset == null)
                return DefaultPagingOffset;

            if (offset < 0)
                throw new RequestFailedApiException(RequestFailedApiException.PagingOffsetInvalid, "The paging offset must be equal to or greater than zero");

            if (offset > ElasticSearchConstants.MaxOffset)
                throw new RequestFailedApiException(RequestFailedApiException.MaxPagingOffsetExceeded, $"The paging offset must not exceed {ElasticSearchConstants.MaxOffset} records");

            return (int)offset;
        }

        public static int ProcessElasticSearchPagingLimit(int? offset, int? limit, int? overrideMaxPagingSize = null)
        {
            if (limit == null)
                return DefaultPagingLimit;

            if (limit < 0)
                throw new RequestFailedApiException(RequestFailedApiException.PagingLimitInvalid, "The paging limit must be equal to or greater than zero");

            int maxPagingSize = overrideMaxPagingSize != null ? (int)overrideMaxPagingSize : ElasticSearchConstants.MaxPagingSize;

            if (limit > maxPagingSize)
                throw new RequestFailedApiException(RequestFailedApiException.MaxPagingLimitExceeded, $"The paging limit must not exceed {maxPagingSize} records");

            return (int)limit;
        }

        public static int ProcessSqlServerPagingOffset(int? offset, int? limit)
        {
            if (offset == null)
                return DefaultPagingOffset;

            if (offset < 0)
                throw new RequestFailedApiException(RequestFailedApiException.PagingOffsetInvalid, "The paging offset must be equal to or greater than zero");

            return (int)offset;
        }

        public static int ProcessSqlServerPagingLimit(int? offset, int? limit)
        {
            if (limit == null)
                return DefaultPagingLimit;

            if (limit < 0)
                throw new RequestFailedApiException(RequestFailedApiException.PagingLimitInvalid, "The paging limit must be equal to or greater than zero");

            if (limit > SqlDatabaseConstants.MaxPagingSize)
                throw new RequestFailedApiException(RequestFailedApiException.MaxPagingLimitExceeded, $"The paging limit must not exceed {SqlDatabaseConstants.MaxPagingSize} records");

            return (int)limit;
        }

        public static int ProcessUnlimitedPagingOffset(int? offset, int? limit)
        {
            if (offset == null)
                return DefaultPagingOffset;

            if (offset < 0)
                throw new RequestFailedApiException(RequestFailedApiException.PagingOffsetInvalid, "The paging offset must be equal to or greater than zero");

            return (int)offset;
        }
    }
}
