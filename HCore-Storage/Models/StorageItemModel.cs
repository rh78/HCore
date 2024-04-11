using System;

namespace HCore.Storage.Models
{
    public class StorageItemModel
    {
        public string Name { get; internal set; }

        public DateTimeOffset? CreatedOn { get; internal set; }

        public DateTimeOffset? LastModified { get; internal set; }

        public DateTimeOffset? LastAccessedOn { get; internal set; }

        public long? ContentLength { get; internal set; }

        public string ContentType { get; internal set; }
    }
}
