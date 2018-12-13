using System.Collections.Generic;
using System.Linq;

namespace BridgeBotNext.Attachments
{
    public class PhotoAlbumAttachment: Attachment
    {
        public PhotoAttachment[] Photos { get; }

        public PhotoAlbumAttachment(object meta, PhotoAttachment[] photos) : base(meta)
        {
            Photos = photos;
        }

        public PhotoAlbumAttachment(string url, object meta, PhotoAttachment[] photos) : base(url, meta)
        {
            Photos = photos;
        }

        public override string ToString()
        {
            return string.Join(System.Environment.NewLine, Photos.Select(p => p.ToString()));
        }
    }
}