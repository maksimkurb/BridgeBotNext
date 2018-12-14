namespace BridgeBotNext.Attachments
{
    public class AudioAttachment : FileAttachment
    {
        public AudioAttachment(
            string url = null,
            object meta = null,
            string caption = null,
            string fileName = null,
            long fileSize = 0,
            string mimeType = null,
            string title = null,
            string performer = null,
            int duration = 0
        ) : base(url, meta, caption, fileName, fileSize, mimeType)
        {
            Title = title;
            Performer = performer;
            Duration = duration;
        }

        public string Title { get; }
        public string Performer { get; }
        public int Duration { get; }
    }
}