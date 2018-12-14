namespace BridgeBotNext.Attachments
{
    public class LinkAttachment: Attachment
    {
        public LinkAttachment(object meta) : base(meta)
        {
        }

        public LinkAttachment(string url, object meta) : base(url, meta)
        {
        }

        public override string ToString()
        {
            return Url;
        }
    }
}