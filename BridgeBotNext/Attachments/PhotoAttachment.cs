using BridgeBotNext.Providers.Tg;

namespace BridgeBotNext.Attachments
{
    public class PhotoAttachment : FileAttachment, IAlbumAttachment
    {
        
        public PhotoAttachment(string url, object meta) : base(url, meta)
        {
        }


        public PhotoAttachment(string url,
            object meta = null,
            string caption = null,
            string fileName = null,
            long fileSize = 0,
            string mimeType = null) : base(url, meta, caption, fileName, fileSize, mimeType)
        {
        }

    }
}