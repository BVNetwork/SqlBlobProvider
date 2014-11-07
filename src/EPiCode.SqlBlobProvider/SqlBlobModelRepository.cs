using System;
using System.Linq;
using EPiServer.Data.Dynamic;
namespace EPiCode.SqlBlobProvider
{
    public class SqlBlobModelRepository
    {
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
                var blobs = (from b in SqlBlobStore.Items<SqlBlobModel>()
                             where ((string)(object)b.BlobId).Contains(id.Segments[1])
                            select b).ToList();
                foreach (var blob in blobs)
                {
                    SqlBlobStore.Delete(blob.Id);
                }
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