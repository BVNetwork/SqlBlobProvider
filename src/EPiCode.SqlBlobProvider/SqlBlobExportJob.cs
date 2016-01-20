using System.IO;
using EPiServer.Framework.Blobs;
using EPiServer.PlugIn;

namespace EPiCode.SqlBlobProvider
{
    [ScheduledPlugIn(DisplayName = "Export SQL Blobs", Description = "Exports all SQL Blobs to disk", SortIndex = 10001)]
    public class ExportSqlBlobsJob : EPiServer.Scheduler.ScheduledJobBase
    {
        private bool _stopSignaled;
        public ExportSqlBlobsJob()
        {
            IsStoppable = true;
        }
        public override void Stop()
        {
            _stopSignaled = true;
        }

        public override string Execute()
        {
            OnStatusChanged("Export started.");
            string saveDirectory = null;
            var store = SqlBlobModelRepository.SqlBlobStore;
            int exported = 0;
            var items = store.Items<SqlBlobModel>();
            foreach (SqlBlobModel item in items)
            {
                if (_stopSignaled)
                {
                    break;
                }
                if (saveDirectory == null)
                {
                    var provider = BlobFactory.Instance.GetProvider(item.BlobId) as SqlBlobProvider;
                    if (provider != null)
                        saveDirectory = provider.Path;
                }
                var id = item.BlobId;
                var path = saveDirectory + id.Segments[0] + id.Segments[1] + id.Segments[2].TrimEnd('\\');
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (item.Blob != null)
                {

                    var stream = new MemoryStream(item.Blob);
                    var fileStream = File.Create(path);
                    stream.CopyTo(fileStream);
                    fileStream.Close();
                    stream.Close();
                }
                exported++;
                if (exported % 50 == 0)
                    OnStatusChanged(string.Format("Exported {0} blobs.", exported));
            }
            string status = string.Format("Job has completed. {0} SQL blobs has been exported to {1}.", exported, saveDirectory);
            OnStatusChanged(status);
            return status;
        }
    }
}
