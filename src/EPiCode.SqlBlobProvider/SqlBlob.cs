using System;
using System.IO;
using EPiServer.Framework.Blobs;

namespace EPiCode.SqlBlobProvider
{
    public class SqlBlob : Blob
    {
        public SqlBlob(Uri id)
            : base(id)
        {

        }

        public override Stream OpenRead()
        {
            return new MemoryStream(SqlBlobModelRepository.Get(base.ID).Blob);
        }

        public override Stream OpenWrite()
        {
            string tempFileName = Path.GetTempFileName();
            var trackableStream = new TrackableStream(new FileStream(tempFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None));
            trackableStream.Closing += ((source, e) =>
            {
                Stream stream = ((TrackableStream)source).InnerStream;
                stream.Seek(0, SeekOrigin.Begin);
                Write(stream);
            });
            trackableStream.Closed += ((source, e) =>
            {
                var file = new FileInfo(tempFileName);
                if (file.Exists)
                {
                    file.Delete();
                }
            });
            return trackableStream;
        }

        public override void Write(Stream stream)
        {
            var sqlBlobModel = SqlBlobModelRepository.Get(base.ID) ?? new SqlBlobModel { BlobId = ID };
            using (var streamReader = new MemoryStream())
            {
                stream.CopyTo(streamReader);
                sqlBlobModel.Blob = streamReader.ToArray();
            }
            SqlBlobModelRepository.Save(sqlBlobModel);
        }
    }
}