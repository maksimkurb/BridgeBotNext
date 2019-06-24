using System;
using System.Text;

namespace BridgeBotNext.Attachments
{
    public class DurationFileAttachment : FileAttachment
    {
        public DurationFileAttachment(string url, object meta = null, string caption = null, string fileName = null,
            long? fileSize = null, int? duration = null, string mimeType = null, string defaultMimeType = null) :
            base(url, meta, caption, fileName, fileSize, mimeType, defaultMimeType)
        {
            Duration = duration;
        }

        public int? Duration { get; }

        protected string ReadableDuration()
        {
            var sb = new StringBuilder();
            var t = TimeSpan.FromSeconds(Duration ?? 0);
            if (t.Hours > 0) sb.AppendFormat("{0:D2}:", t.Hours);

            sb.AppendFormat("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
            return sb.ToString();
        }
    }
}