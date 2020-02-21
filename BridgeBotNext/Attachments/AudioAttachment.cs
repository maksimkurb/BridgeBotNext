using System.Text;

namespace BridgeBotNext.Attachments
{
    public class AudioAttachment : DurationFileAttachment
    {
        public AudioAttachment(
            string url = null,
            object meta = null,
            string caption = null,
            string fileName = null,
            long fileSize = 0,
            string mimeType = null,
            string defaultMimeType = "audio/mpeg",
            string title = null,
            string performer = null,
            ulong? duration = null
        ) : base(url, meta, caption, fileName, fileSize, duration, mimeType, defaultMimeType)
        {
            Title = title;
            Performer = performer;
        }

        public string Title { get; }
        public string Performer { get; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(Performer))
            {
                sb.Append(Performer);
                sb.Append(" - ");
            }

            sb.Append(!string.IsNullOrEmpty(Title) ? Title : "[audio]");

            if (Duration > 5) sb.Append(ReadableDuration());

            return sb.ToString();
        }
    }
}