using System;
using System.IO;
using log4net;

namespace EPiCode.SqlBlobProvider
{
    public class FileHelper
    {
        protected static ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        internal static void Delete(Uri id, string path)
        {
            try
            {
                if (id.Segments.Length == 2)
                {
                    var directoryInfo = new DirectoryInfo(Path.Combine(path, id.AbsolutePath.Substring(1)));
                    if (!directoryInfo.Exists)
                        return;
                    directoryInfo.Delete(true);
                }
                else
                {
                    if (id.Segments.Length != 3)
                        return;
                    var fileInfo = new FileInfo(Path.Combine(path, id.AbsolutePath.Substring(1)));
                    if (!fileInfo.Exists)
                        return;
                    fileInfo.Delete();
                }
            }
            catch (Exception ex)
            {
                Log.Error("An error occured while deleting SqlBlobProvider files", ex);
            }
        }

        internal static Stream GetOrCreateFileBlob(string filePath, Uri id)
        {
            if (File.Exists(filePath))
            {
                return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            var bytes = SqlBlobModelRepository.Get(id).Blob;
            var directoryInfo = new DirectoryInfo(Path.GetDirectoryName(filePath));
            if (!directoryInfo.Exists)
                directoryInfo.Create();

            File.WriteAllBytes(filePath, bytes);
            return new MemoryStream(bytes);
        }
    }
}
