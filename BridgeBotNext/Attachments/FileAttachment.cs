using System;
using System.IO;
using HeyRed.Mime;

namespace BridgeBotNext.Attachments
{
    public class FileAttachment : Attachment
    {
        public FileAttachment(string url, object meta = null, string caption = null, string fileName = null,
            long? fileSize = 0, string mimeType = null, string defaultMimeType = null) : base(url, meta)
        {
            Caption = caption;

            if (string.IsNullOrEmpty(mimeType))
            {
                FileName = string.IsNullOrEmpty(fileName)
                    ? _getFilename(url, defaultMimeType: defaultMimeType)
                    : fileName;
                MimeType = MimeTypesMap.GetMimeType(FileName);
            }
            else
            {
                MimeType = mimeType;
                FileName = string.IsNullOrEmpty(fileName) ? _getFilename(url, mimeType) : fileName;
            }

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
        public virtual long? FileSize { get; }
        public string ReadableFileSize => _readableFileSize();


        private static string _getFilename(string url, string mimeType = null, string defaultMimeType = null)
        {
            var fileName = Path.GetFileName(new Uri(url).LocalPath);
            if (string.IsNullOrEmpty(fileName)) fileName = "noname";
            if (!fileName.Contains("."))
            {
                if (mimeType != null)
                    return $"{fileName}.{MimeTypesMap.GetExtension(mimeType)}";
                if (defaultMimeType != null) return $"{fileName}.{MimeTypesMap.GetExtension(defaultMimeType)}";
            }

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
            var fileSize = FileSize ?? 0;
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absoluteI >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = fileSize >> 50;
            }
            else if (absoluteI >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = fileSize >> 40;
            }
            else if (absoluteI >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = fileSize >> 30;
            }
            else if (absoluteI >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = fileSize >> 20;
            }
            else if (absoluteI >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = fileSize >> 10;
            }
            else if (absoluteI >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = fileSize;
            }
            else
            {
                return fileSize.ToString("0 B"); // Byte
            }

            // Divide by 1024 to get fractional value
            readable = readable / 1024;
            // Return formatted number with suffix
            return $"{readable:0.###}{suffix}";
        }

        public override string ToString()
        {
            return $"{FileName} ({ReadableFileSize}) {Url}\n{Caption}";
        }
    }
}