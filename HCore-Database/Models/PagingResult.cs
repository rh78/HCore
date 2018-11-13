using System.Collections.Generic;

namespace HCore.Database.Models
{
    public class PagingResult<TResult>
    {
        public List<TResult> Result { get; set; }

        public int TotalCount { get; set; }
    }
}
