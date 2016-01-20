using System.IO;

namespace EPiCode.SqlBlobProvider
{
    public class NonSeekableMemoryStream : MemoryStream
    {
        public NonSeekableMemoryStream(byte[] blob) : base(blob)
        {
            
        }

        /// <summary>
        /// By always returning false, we effectively turn off
        /// Episerver's async download support
        /// </summary>
        public override bool CanSeek
        {
            get { return false; }
        }
    }
}