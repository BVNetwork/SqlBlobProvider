using System;
using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace EPiCode.SqlBlobProvider
{
    [EPiServerDataStore(AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
    public class SqlBlobModel
    {
        public SqlBlobModel()
        {
            Id = Guid.NewGuid();
        }
        public Uri BlobId { get; set; }
        public Identity Id { get; set; }
        public byte[] Blob { get; set; }
    }
}