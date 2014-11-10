using EPiServer;
using EPiServer.Framework;
using EPiServer.Web;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Framework.Blobs;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace EPiCode.SqlBlobProvider
{
    public class SqlBlobProvider : BlobProvider
    {
        public string Path { get; internal set; }
        public bool LoadFromDisk { get; internal set; }
        private const string PathKey = "path";
        private const string LoadFromDiskKey = "loadFromDisk";

        public SqlBlobProvider()
            : this("[appDataPath]\\sqlProviderBlobs",false)
        {

        }
        public SqlBlobProvider(string path, bool loadFromDisk)
        {
            LoadFromDisk = loadFromDisk;
            Path = VirtualPathUtilityEx.RebasePhysicalPath(path);
        }
        public override void Initialize(string name, NameValueCollection config)
        {
            if (config.Get(PathKey) != null)
                Path = VirtualPathUtilityEx.RebasePhysicalPath(config.Get(PathKey));
            Validator.ThrowIfNullOrEmpty(PathKey, this.Path);
            if (config.Get(LoadFromDiskKey) != null)
                LoadFromDisk = bool.Parse((config.Get(LoadFromDiskKey)));           
            var events = ServiceLocator.Current.GetInstance<IContentEvents>();
            events.DeletingContent += DeleteSqlBlobProviderFiles;
            base.Initialize(name, config);
        }

        private void DeleteSqlBlobProviderFiles(object sender, DeleteContentEventArgs e)
        {
            if (e.DeletedDescendents == null || e.DeletedDescendents.Any())
                return;
            var contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            foreach (var descendant in e.DeletedDescendents)
            {
                MediaData mediaData;
                if (contentRepository.TryGet(descendant, out mediaData))
                {
                    FileHelper.Delete(Blob.GetContainerIdentifier(mediaData.ContentGuid), Path);
                }
            }
        }

        public override Blob GetBlob(Uri id)
        {
            return new SqlBlob(id, System.IO.Path.Combine(this.Path, id.AbsolutePath.Substring(1)), LoadFromDisk);
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
            FileHelper.Delete(id, this.Path);
        }
    }
}
