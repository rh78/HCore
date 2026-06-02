using System;
using System.Collections.Generic;

namespace HCore.Database.Models
{
    [Serializable]
    public class PagingResult<TResult>
    {
        public List<TResult> Result { get; set; }

        public int TotalCount { get; set; }

        public List<TagFragment> TagFragments { get; set; }
    }

    [Serializable]
    public class TagFragment
    {
        public string Tag { get; set; }

        public long? Count { get; set; }
    }
}
