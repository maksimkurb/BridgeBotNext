using System.Collections.Generic;
using System.Linq;

namespace BridgeBotNext.Attachments
{
    public class AlbumAttachment: Attachment
    {
        public IEnumerable<IAlbumAttachment> Media { get; }

        public AlbumAttachment(object meta, IEnumerable<IAlbumAttachment> media) : base(meta)
        {
            Media = media;
        }

        public AlbumAttachment(string url, object meta, IEnumerable<IAlbumAttachment> media) : base(url, meta)
        {
            Media = media;
        }

        public override string ToString()
        {
            return string.Join(System.Environment.NewLine, Media.Select(p => p.ToString()));
        }
    }
}