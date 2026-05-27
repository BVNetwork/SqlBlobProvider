using System;
using System.IO;
using EPiServer.Framework.Blobs;
using Microsoft.Extensions.FileProviders;
using System.Threading.Tasks;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EPiCode.SqlBlobProvider;

public class SqlBlob : Blob
{
    public string FilePath { get; internal set; }
    public bool LoadFromDisk { get; internal set; }

    public SqlBlob(Uri id, string filePath, bool loadFromDisk)
        : base(id)
    {
        FilePath = filePath;
        LoadFromDisk = loadFromDisk;
    }

    public override Stream OpenRead()
    {
        if (!LoadFromDisk)
        {
            var blobModel = SqlBlobModelRepository.Get(ID);
            if (blobModel?.Blob == null)
            {
                throw new FileNotFoundException($"Blob not found for id '{ID}'.", ID.ToString());
            }

            return new NonSeekableMemoryStream(blobModel.Blob);
        }

        return FileHelper.GetOrCreateFileBlob(FilePath, ID);
    }

    public override Task<IFileInfo> AsFileInfoAsync(DateTimeOffset? lastModified = null)
    {
        var exists = false;
        long length = -1;

        if (LoadFromDisk && !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
        {
            var fileInfo = new FileInfo(FilePath);
            exists = true;
            length = fileInfo.Length;
        }
        else
        {
            var blobModel = SqlBlobModelRepository.Get(ID);
            if (blobModel?.Blob != null)
            {
                exists = true;
                length = blobModel.Blob.LongLength;
            }
        }

        return Task.FromResult<IFileInfo>(new SqlBlobFileInfo(this, exists, length, lastModified));
    }

    public override Stream OpenWrite()
    {
        var tempFileName = Path.GetTempFileName();
        var trackableStream = new TrackableStream(new FileStream(tempFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None));
        trackableStream.Closing += delegate(object source, EventArgs _)
        {
            var innerStream = ((TrackableStream)source).InnerStream;
            innerStream.Seek(0L, SeekOrigin.Begin);
            Write(innerStream);
        };
        trackableStream.Closed += (_,_) =>
        {
            var fileInfo = new FileInfo(tempFileName);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
        };
        return trackableStream;
    }

    public override void Write(Stream stream)
    {
        SqlBlobModel blobModel;
        if ((blobModel = SqlBlobModelRepository.Get(ID)) == null)
        {
            blobModel = new()
            {
                BlobId = ID,
            };
        }

        var sqlBlobModel = blobModel;
        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            sqlBlobModel.Blob = memoryStream.ToArray();
        }

        SqlBlobModelRepository.Save(sqlBlobModel);
    }

    private sealed class SqlBlobFileInfo : IFileInfo
    {
        private readonly SqlBlob _blob;
        private readonly DateTimeOffset? _lastModified;

        public SqlBlobFileInfo(SqlBlob blob, bool exists, long length, DateTimeOffset? lastModified)
        {
            _blob = blob;
            Exists = exists;
            Length = length;
            _lastModified = lastModified;
        }

        public bool Exists { get; }
        public long Length { get; }
        public string PhysicalPath => _blob.FilePath;
        public string Name => Path.GetFileName(_blob.FilePath);
        public DateTimeOffset LastModified => _lastModified ?? DateTimeOffset.UtcNow;
        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            if (!Exists)
            {
                throw new FileNotFoundException($"Blob not found for id '{_blob.ID}'.", _blob.ID.ToString());
            }

            return _blob.OpenRead();
        }
    }
}