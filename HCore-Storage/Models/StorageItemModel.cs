using System;

namespace HCore.Storage.Models
{
    public class StorageItemModel
    {
        public string Name { get; set; }

        public DateTimeOffset? CreatedOn { get; set; }

        public DateTimeOffset? LastModified { get; set; }

        public DateTimeOffset? LastAccessedOn { get; set; }

        public long? ContentLength { get; set; }

        public string ContentType { get; set; }
    }
}
