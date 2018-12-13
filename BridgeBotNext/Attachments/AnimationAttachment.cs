namespace BridgeBotNext.Attachments
{
    public class AnimationAttachment : VideoAttachment
    {
        public AnimationAttachment(
            string url,
            object meta = null,
            string fileName = null,
            long fileSize = 0,
            string mimeType = "text/plain",
            string title = null,
            int duration = 0,
            int width = 0,
            int height = 0
        ) : base(url, meta, fileName, fileSize, mimeType, title, duration, width, height)
        {
        }
    }
}