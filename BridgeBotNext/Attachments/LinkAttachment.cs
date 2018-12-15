namespace BridgeBotNext.Attachments
{
    public class LinkAttachment : Attachment
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