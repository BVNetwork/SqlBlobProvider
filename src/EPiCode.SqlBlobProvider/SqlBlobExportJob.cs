using System.IO;
using EPiServer.Framework.Blobs;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;

namespace EPiCode.SqlBlobProvider;

[ScheduledPlugIn(
    DisplayName = "Export SQL Blobs",
    Description = "Exports all SQL Blobs to disk",
    SortIndex = 10001)]
public class ExportSqlBlobsJob : ScheduledJobBase
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
        var exported = 0;
        var items = store.Items<SqlBlobModel>();
        foreach (var item in items)
        {
            if (_stopSignaled)
            {
                break;
            }
            if (saveDirectory == null)
            {
                var blobProviderRegistry = ServiceLocator.Current.GetInstance<IBlobProviderRegistry>();
                if(blobProviderRegistry.GetProvider(item.BlobId) is SqlBlobProvider provider) {
                    saveDirectory = provider.Path;
                }
            }
            var id = item.BlobId;
            var path = saveDirectory + id.Segments[0] + id.Segments[1] + id.Segments[2].TrimEnd('\\');
            var directoryName = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directoryName))
            {
                continue;
            }
            Directory.CreateDirectory(directoryName);
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
            {
                OnStatusChanged($"Exported {exported} blobs.");
            }
        }
        var status = $"Job has completed. {exported} SQL blobs has been exported to {saveDirectory}.";
        OnStatusChanged(status);
        return status;
    }
}