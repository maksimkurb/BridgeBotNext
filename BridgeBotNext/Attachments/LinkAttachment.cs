using System.Text;
using BridgeBotNext.Providers.Vk;

namespace BridgeBotNext.Attachments
{
    public class LinkAttachment : Attachment, IVkSpecialAttachment
    {
        public LinkAttachment(string url, object meta = null, string title = null) : base(url, meta)
        {
            Title = title;
        }

        public string Title { get; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(Title))
            {
                sb.AppendLine(Title);
            }

            sb.Append(Url);

            return sb.ToString();
        }
    }
}