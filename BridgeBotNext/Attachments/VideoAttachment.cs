namespace BridgeBotNext.Attachments
{
    public class VideoAttachment : DurationFileAttachment, ITgGroupableAttachment
    {
        public VideoAttachment(
            string url,
            object meta = null,
            string caption = null,
            string fileName = null,
            long fileSize = 0,
            string mimeType = null,
            string title = null,
            ulong duration = 0,
            int width = 0,
            int height = 0
        ) : base(url, meta, caption, fileName, fileSize, duration, mimeType, "video/mp4")
        {
            Title = title;
            Width = width;
            Height = height;
        }

        public string Title { get; }
        public int Width { get; }
        public int Height { get; }
    }
}