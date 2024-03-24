using System;
using System.Linq;
using System.Threading.Tasks;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Blobs;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Microsoft.Extensions.Configuration;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace EPiCode.SqlBlobProvider;

public class SqlBlobProvider : BlobProvider
{
    public string Path { get; internal set; }
    public bool LoadFromDisk { get; internal set; }
    private const string PathKey = "Episerver:CMS:SqlBlobProvider:Path";
    private const string LoadFromDiskKey = "Episerver:CMS:SqlBlobProvider:LoadFromDisk";
    protected const string StandardFilePath = "[appDataPath]\\sqlProviderBlobs";

    public SqlBlobProvider() : this(StandardFilePath, false)
    {
    }

    public SqlBlobProvider(string path, bool loadFromDisk)
    {
        LoadFromDisk = loadFromDisk;
        Path = VirtualPathUtilityEx.RebasePhysicalPath(path);
    }

    public override Task InitializeAsync()
    {
        var configuration = ServiceLocator.Current.GetInstance<IConfiguration>();
        var path = configuration.GetValue<string>(PathKey);
        LoadFromDisk = configuration.GetValue<bool>(LoadFromDiskKey);

        if (!string.IsNullOrWhiteSpace(path))
        {
            Path = VirtualPathUtilityEx.RebasePhysicalPath(path);
        }

        Validator.ThrowIfNullOrEmpty(PathKey, Path);

        var events = ServiceLocator.Current.GetInstance<IContentEvents>();
        if (LoadFromDisk)
        {
            events.DeletingContent += DeleteSqlBlobProviderFiles;
        }

        return Task.FromResult((object)null);
    }

    private void DeleteSqlBlobProviderFiles(object sender, DeleteContentEventArgs e)
    {
        if (e.DeletedDescendents == null || e.DeletedDescendents.Any())
        {
            return;
        }

        var contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
        foreach (var descendant in e.DeletedDescendents)
        {
            if (contentRepository.TryGet(descendant, out MediaData mediaData))
            {
                FileHelper.Delete(Blob.GetContainerIdentifier(mediaData.ContentGuid), Path);
            }
        }
    }

    public override Blob GetBlob(Uri id)
    {
        return new SqlBlob(id, System.IO.Path.Combine(Path, id.AbsolutePath[1..]), LoadFromDisk);
    }

    public override Blob CreateBlob(Uri id, string extension)
    {
        var sqlBlobModel = new SqlBlobModel
        {
            BlobId = Blob.NewBlobIdentifier(id, extension)
        };
        SqlBlobModelRepository.Save(sqlBlobModel);
        return GetBlob(sqlBlobModel.BlobId);
    }

    public override void Delete(Uri id)
    {
        SqlBlobModelRepository.Delete(id);
        FileHelper.Delete(id, Path);
    }
}