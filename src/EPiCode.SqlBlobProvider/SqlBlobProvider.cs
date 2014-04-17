using System;
using EPiServer.Framework.Blobs;

namespace EPiCode.SqlBlobProvider
{
    public class SqlBlobProvider : BlobProvider
    { 
        public override Blob GetBlob(Uri id)
        { 
            return new SqlBlob(id);
        }

        public override Blob CreateBlob(Uri id, string extension)
        {
            var blob = new SqlBlobModel
            {
                BlobId = (Blob.NewBlobIdentifier(id, extension))      
            };
            SqlBlobModelRepository.Save(blob);
            return GetBlob(blob.BlobId);
        }

        public override void Delete(Uri id)
        {
            SqlBlobModelRepository.Delete(id);
        }
    }
}