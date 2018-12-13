namespace BridgeBotNext.Attachments
{
    public class PhotoAttachment : FileAttachment
    {
        public string Description { get; }
        
        public PhotoAttachment(string url, object meta) : base(url, meta)
        {
        }


        public PhotoAttachment(string url,
            object meta = null,
            string fileName = null,
            long fileSize = 0,
            string mimeType = null) : base(url, meta, fileName, fileSize, mimeType)
        {
        }

        public PhotoAttachment(string description,
            string url,
            object meta = null,
            string fileName = null,
            long fileSize = 0,
            string mimeType = null) : base(url, meta, fileName, fileSize, mimeType)
        {
            Description = description;
        }

    }
}