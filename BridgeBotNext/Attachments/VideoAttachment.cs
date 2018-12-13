namespace BridgeBotNext.Attachments
{
    public class VideoAttachment : FileAttachment
    {
        public VideoAttachment(
            string url,
            object meta = null,
            string fileName = null,
            long fileSize = 0,
            string mimeType = "text/plain",
            string title = null,
            int duration = 0,
            int width = 0,
            int height = 0
        ) : base(url, meta, fileName, fileSize, mimeType)
        {
            Title = title;
            Duration = duration;
            Width = width;
            Height = height;
        }

        public string Title { get; }
        public int Duration { get; }
        public int Width { get; }
        public int Height { get; }
    }
}