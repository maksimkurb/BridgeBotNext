namespace BridgeBotNext.Attachments
{
    public class PhotoAttachment : FileAttachment, ITgGroupableAttachment
    {
        public PhotoAttachment(string url, object meta) : base(url, meta, defaultMimeType: "image/jpeg")
        {
        }


        public PhotoAttachment(string url,
            object meta = null,
            string caption = null,
            string fileName = null,
            long fileSize = 0,
            string mimeType = null,
            string defaultMimeType = "image/jpeg") : base(url, meta, caption, fileName, fileSize, mimeType,
            defaultMimeType)
        {
        }
    }
}