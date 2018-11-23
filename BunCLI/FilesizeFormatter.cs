using System;
using System.Collections.Generic;
using System.Text;

namespace BunCLI
{
    public static class FilesizeFormatter
    {
        private readonly static string[] scale = new string[]
        {
            "B",
            "KiB",
            "MiB",
            "GiB",
            "TiB"
        };

        /// <summary>
        /// Converts a filesize in bytes to a human readable format.
        /// </summary>
        public static string FormatFilesize(long length)
        {
            if (length == 0) { return "0 B"; }

            double xp = Math.Min(Math.Floor(Math.Log(length) / Math.Log(1024)), scale.Length - 1);
            double reduced = length / Math.Pow(1024, xp);
            string decimalFormat = xp == 0 ? "F0" : "F2";
            return $"{reduced.ToString(decimalFormat)} {scale[(int)xp]}";
        }
    }
}
