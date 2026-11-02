using System;
using System.IO;
using EPiServer.Framework.Blobs;
using Microsoft.Extensions.FileProviders;
using System.Threading;
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

    public override Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(OpenRead());
    }

    public override Task<Stream> OpenWriteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(OpenWrite());
    }

    public override async Task WriteAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
            await stream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
            sqlBlobModel.Blob = memoryStream.ToArray();
        }

        SqlBlobModelRepository.Save(sqlBlobModel);
    }

    public override bool Exists()
    {
        if (LoadFromDisk)
        {
            return !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath);
        }

        var blobModel = SqlBlobModelRepository.Get(ID);
        return blobModel?.Blob != null;
    }

    public override Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Exists());
    }

    public override async Task<IFileInfo> AsFileInfoAsync(DateTimeOffset lastModified, bool openReadStream, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var exists = false;
        long length = -1;
        string physicalPath = null;
        Stream readStream = null;

        if (LoadFromDisk && !string.IsNullOrWhiteSpace(FilePath) && File.Exists(FilePath))
        {
            var fileInfo = new FileInfo(FilePath);
            exists = true;
            length = fileInfo.Length;
            physicalPath = fileInfo.FullName;
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

        if (openReadStream && exists)
        {
            readStream = await OpenReadAsync(cancellationToken).ConfigureAwait(false);
        }

        return new SqlBlobFileInfo(this, exists, length, physicalPath, lastModified, readStream);
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
        private readonly DateTimeOffset _lastModified;
        private readonly string _physicalPath;
        private Stream _readStream;

        public SqlBlobFileInfo(SqlBlob blob, bool exists, long length, string physicalPath, DateTimeOffset lastModified, Stream readStream)
        {
            _blob = blob;
            Exists = exists;
            Length = length;
            _physicalPath = physicalPath;
            _lastModified = lastModified;
            _readStream = readStream;
        }

        public bool Exists { get; }
        public long Length { get; }
        public string PhysicalPath => _physicalPath;
        public string Name => Path.GetFileName(_blob.FilePath);
        public DateTimeOffset LastModified => _lastModified;
        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            if (!Exists)
            {
                throw new FileNotFoundException($"Blob not found for id '{_blob.ID}'.", _blob.ID.ToString());
            }

            var readStream = Interlocked.Exchange(ref _readStream, null);
            return readStream ?? _blob.OpenRead();
        }
    }
}