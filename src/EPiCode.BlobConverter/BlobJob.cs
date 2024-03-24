using System;
using System.IO;
using System.Text;
using EPiServer.Framework.Blobs;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedType.Global

namespace EPiCode.BlobConverter;

[ScheduledPlugIn(
    DisplayName = "Convert File Blobs",
    Description = "Converts all file blobs into the currently configured blob type",
    SortIndex = 10000)]
public class BlobJob : ScheduledJobBase
{
    protected Injected<IBlobProviderRegistry> BlobProviderRegistry { get; set; }
    private int _count;
    private int _failCount;
    private readonly StringBuilder _errorText = new ();

    public BlobJob()
    {
        IsStoppable = false;
    }

    public override string Execute()
    {
        OnStatusChanged($"Starting execution of {GetType()}");
        ProcessDirectory(new FileBlobProvider().Path);
        var status = $"Converted {_count} blobs <br\\>";
        if (_failCount > 0)
        {
            status = $"Converting errors:{_failCount}. Details:{_errorText}";
        }

        return status;
    }

    private void ProcessFile(string path, string directory)
    {
        try
        {
            path = Path.GetFileName(path);
            directory = Path.GetFileName(directory);
            var id =
                new Uri($"{Blob.BlobUriScheme}://{Blob.DefaultProvider}/{directory}/{path}");
            var blob = new FileBlobProvider().GetBlob(id);

            var blobProvider = BlobProviderRegistry.Service.GetProvider(id);
            blobProvider.GetBlob(id).Write(blob.OpenRead());
            _count++;
            if (_count % 50 == 0)
            {
                OnStatusChanged($"Converted {_count} blobs.");
            }
        }
        catch (Exception ex)
        {
            _failCount++;
            _errorText.AppendLine(ex.ToString());
        }
    }

    private void ProcessDirectory(string targetDirectory)
    {
        foreach (var fileName in Directory.GetFiles(targetDirectory))
        {
            ProcessFile(fileName, targetDirectory);
        }

        foreach (var subdirectory in Directory.GetDirectories(targetDirectory))
        {
            ProcessDirectory(subdirectory);
        }
    }
}