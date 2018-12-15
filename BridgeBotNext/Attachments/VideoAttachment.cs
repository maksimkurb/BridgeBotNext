namespace BridgeBotNext.Attachments
{
    public class VideoAttachment : FileAttachment, IAlbumableAttachment
    {
        public VideoAttachment(
            string url,
            object meta = null,
            string caption = null,
            string fileName = null,
            long fileSize = 0,
            string mimeType = "text/plain",
            string title = null,
            int duration = 0,
            int width = 0,
            int height = 0
        ) : base(url, meta, caption, fileName, fileSize, mimeType)
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