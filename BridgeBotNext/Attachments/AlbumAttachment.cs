using System.Collections.Generic;
using System.Linq;

namespace BridgeBotNext.Attachments
{
    public class AlbumAttachment: Attachment
    {
        public IEnumerable<IAlbumableAttachment> Media { get; }

        public AlbumAttachment(IEnumerable<IAlbumableAttachment> media, object meta) : base(meta)
        {
            Media = media;
        }

        public AlbumAttachment(IEnumerable<IAlbumableAttachment> media, object meta = null, string url = null) : base(url, meta)
        {
            Media = media;
        }

        public override string ToString()
        {
            return string.Join(System.Environment.NewLine, Media.Select(p => p.ToString()));
        }
    }
}