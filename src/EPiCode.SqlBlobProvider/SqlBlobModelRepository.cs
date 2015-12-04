using System;
using System.Linq;
using EPiServer.Data.Dynamic;
using EPiServer.Logging.Compatibility;

namespace EPiCode.SqlBlobProvider
{
    public class SqlBlobModelRepository
    {
        private static ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static SqlBlobModel Get(Uri id)
        {
            var blobModel = SqlBlobStore.Find<SqlBlobModel>("BlobId", id.ToString()).FirstOrDefault();
            return blobModel;
        }

        public static DynamicDataStore SqlBlobStore
        {
            get  
            {
                return DynamicDataStoreFactory.Instance.GetStore(typeof(SqlBlobModel));
            }
        }

        public static void Save(SqlBlobModel blob)
        { 
            SqlBlobStore.Save(blob, blob.Id);
        }

        public static void Delete(Uri id)
        {
            if (id.Segments.Length == 2)
            {
                _log.Debug("Starting delete SQL Blob container " + id);
                var blobs = (from b in SqlBlobStore.Items<SqlBlobModel>()
                             where ((string)(object)b.BlobId).Contains(id.Segments[1])
                            select b).ToList();
                foreach (var blob in blobs)
                {
                    _log.Debug("Deleting SQL Blob " + blob.Id);
                    SqlBlobStore.Delete(blob.Id);
                }
                _log.Debug("Finished deleting SQL Blob container " + id);
            }
            else
            {
                var blobModel = (from b in SqlBlobStore.Items<SqlBlobModel>()
                                 where b.BlobId == id
                                 select b).FirstOrDefault();

                if (blobModel != null)
                    SqlBlobStore.Delete(blobModel.Id);
            }
        }
    }
}