using System;
using System.Collections.Generic;
using System.Text;

namespace BunCLI
{
    public enum Direction
    {
        None,
        Up,
        Down
    };

    public class SyncFile
    {
        public SyncFile(string path, DateTimeOffset lastModified, long length) { Path = path; LastModified = lastModified; Length = length; }
        public string Path { get; set; }
        public DateTimeOffset LastModified { get; set; }
        /// <summary>
        /// The size of the file in bytes.
        /// </summary>
        public long Length { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is SyncFile s)
            {
                return Path == s.Path;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }
    }
}
