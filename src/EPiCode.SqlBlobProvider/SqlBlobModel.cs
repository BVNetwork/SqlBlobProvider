using System;
using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace EPiCode.SqlBlobProvider;

[EPiServerDataStore(AutomaticallyCreateStore = true, AutomaticallyRemapStore = true)]
public class SqlBlobModel
{
    [EPiServerDataIndex] public Uri BlobId { get; set; }
    public Identity Id { get; set; } = Guid.NewGuid();
    public byte[] Blob { get; set; }
}