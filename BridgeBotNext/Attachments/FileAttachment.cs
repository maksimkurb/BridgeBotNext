using System;
using System.IO;
using HeyRed.Mime;

namespace BridgeBotNext.Attachments
{
    public class FileAttachment : Attachment
    {
        public FileAttachment(string url, object meta = null, string caption = null, string fileName = null,
            long fileSize = 0,
            string mimeType = null) : base(url, meta)
        {
            Caption = caption;
            FileName = fileName ?? _generateDefaultFileName(url, mimeType);
            MimeType = mimeType ?? MimeTypesMap.GetMimeType(FileName);
            FileSize = fileSize;
        }

        public FileAttachment(string url, object meta) : base(url, meta)
        {
        }

        public FileAttachment(object meta) : base(meta)
        {
        }

        public string Caption { get; }
        public string FileName { get; }
        public virtual string MimeType { get; }
        public virtual long FileSize { get; }
        public string ReadableFileSize => _readableFileSize();


        private static string _generateDefaultFileName(string url, string mimeType)
        {
            var fileName = Path.GetFileName(new Uri(url).LocalPath);
            if (string.IsNullOrEmpty(fileName)) fileName = "noname";
            if (!string.IsNullOrEmpty(mimeType) && MimeTypesMap.GetMimeType(fileName) != mimeType.ToLower())
                return $"{fileName}.{MimeTypesMap.GetExtension(mimeType)}";

            return fileName;
        }

        /**
         * Get human-readable file size
         * @url https://www.somacon.com/p576.php
         */
        private string _readableFileSize()
        {
            // Get absolute value
            var absoluteI = FileSize < 0 ? -FileSize : FileSize;
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absoluteI >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = FileSize >> 50;
            }
            else if (absoluteI >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = FileSize >> 40;
            }
            else if (absoluteI >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = FileSize >> 30;
            }
            else if (absoluteI >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = FileSize >> 20;
            }
            else if (absoluteI >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = FileSize >> 10;
            }
            else if (absoluteI >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = FileSize;
            }
            else
            {
                return FileSize.ToString("0 B"); // Byte
            }

            // Divide by 1024 to get fractional value
            readable = readable / 1024;
            // Return formatted number with suffix
            return string.Format("{0:0.### }{0}", readable, suffix);
        }

        public override string ToString()
        {
            return $"{FileName} ({ReadableFileSize}) {Url}\n{Caption}";
        }
    }
}