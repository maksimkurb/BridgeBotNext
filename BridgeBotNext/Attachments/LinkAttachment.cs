using BridgeBotNext.Providers.Vk;

namespace BridgeBotNext.Attachments
{
    public class LinkAttachment : Attachment, IVkSpecialAttachment
    {
        public LinkAttachment(string url, object meta = null) : base(url, meta)
        {
        }

        public override string ToString()
        {
            return Url;
        }
    }
}