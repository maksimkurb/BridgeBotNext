namespace BridgeBotNext.Attachments
{
    public class StickerAttachment : FileAttachment
    {
        public StickerAttachment(string url, object meta) : base(url, meta)
        {
        }

        public StickerAttachment(string url, object meta = null, string fileName = null, long fileSize = 0,
            string mimeType = null) : base(url, meta, null, fileName, fileSize, mimeType, "image/png")
        {
        }
    }
}