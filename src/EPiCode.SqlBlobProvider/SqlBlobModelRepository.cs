using System;
using System.Linq;
using EPiServer.Data.Dynamic;
using EPiServer.Logging;
// ReSharper disable InconsistentNaming
// ReSharper disable ClassNeverInstantiated.Global

namespace EPiCode.SqlBlobProvider;

public class SqlBlobModelRepository
{
    private static readonly ILogger _log = LogManager.Instance.GetLogger(nameof(SqlBlobModelRepository));

    public static SqlBlobModel Get(Uri id)
    {
        var blobModel = SqlBlobStore.Find<SqlBlobModel>(nameof(SqlBlobModel.BlobId), id.ToString()).FirstOrDefault();
        return blobModel;
    }

    public static DynamicDataStore SqlBlobStore => DynamicDataStoreFactory.Instance.GetStore(typeof(SqlBlobModel));

    public static void Save(SqlBlobModel blob)
    {
        SqlBlobStore.Save(blob, blob.Id);
    }

    public static void Delete(Uri id)
    {
        if (id.Segments.Length == 2)
        {
            _log.Debug("Starting delete SQL Blob container " + id);
            var blobs = SqlBlobStore.Items<SqlBlobModel>()
                .Where(b => ((string)(object)b.BlobId).Contains(id.Segments[1]))
                .ToList();

            foreach (var blob in blobs)
            {
                _log.Debug("Deleting SQL Blob " + blob.Id);
                SqlBlobStore.Delete(blob.Id);
            }

            _log.Debug("Finished deleting SQL Blob container " + id);
        }
        else
        {
            var blobModel = SqlBlobStore.Items<SqlBlobModel>()
                .FirstOrDefault(b => b.BlobId == id);

            if (blobModel != null)
            {
                SqlBlobStore.Delete(blobModel.Id);
            }
        }
    }
}