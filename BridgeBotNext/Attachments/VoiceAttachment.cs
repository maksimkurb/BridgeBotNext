namespace BridgeBotNext.Attachments
{
    public class VoiceAttachment : AudioAttachment
    {
        public VoiceAttachment(string url = null, object meta = null, string caption = null, string fileName = null,
            long fileSize = 0,
            string mimeType = null, string title = null, ulong duration = 0) : base(url, meta, caption, fileName,
            fileSize,
            mimeType, "audio/ogg", title, null, duration)
        {
        }
    }
}