namespace BridgeBotNext.Attachments
{
    public class AnimationAttachment : VideoAttachment
    {
        public AnimationAttachment(
            string url,
            object meta = null,
            string caption = null,
            string fileName = null,
            long fileSize = 0,
            string mimeType = "text/plain",
            int duration = 0,
            int width = 0,
            int height = 0
        ) : base(url, meta, caption, fileName, fileSize, mimeType, null, duration, width, height)
        {
        }
    }
}