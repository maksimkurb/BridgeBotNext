namespace BridgeBotNext.Attachments
{
    public class AudioAttachment : FileAttachment
    {
        public AudioAttachment(
            string url = null,
            object meta = null,
            string fileName = null,
            long fileSize = 0,
            string mimeType = null,
            string title = null,
            int duration = 0
        ) : base(url, meta, fileName, fileSize, mimeType)
        {
            Title = title;
            Duration = duration;
        }

        public string Title { get; }
        public int Duration { get; }
    }
}