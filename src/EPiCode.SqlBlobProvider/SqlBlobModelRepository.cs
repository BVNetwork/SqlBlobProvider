using System;
using System.Linq;
using EPiServer.Data.Dynamic;

namespace EPiCode.SqlBlobProvider
{
    public class SqlBlobModelRepository
    {
        public static SqlBlobModel Get(Uri id)
        {
            var comment = (from c in SqlBlobStore.Items<SqlBlobModel>()
                           where c.BlobId == id
                           select c).FirstOrDefault();
            return comment;
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
            var comment = (from c in SqlBlobStore.Items<SqlBlobModel>()
                           where c.BlobId == id
                           select c).FirstOrDefault();

            if (comment != null)
                SqlBlobStore.Delete(comment.BlobId);
        }
    }
}