namespace BridgeBotNext.Attachments
{
    public class VoiceAttachment : AudioAttachment
    {
        public VoiceAttachment(string url = null, object meta = null, string fileName = null, long fileSize = 0,
            string mimeType = null, string title = null, int duration = 0) : base(url, meta, fileName, fileSize,
            mimeType, title, duration)
        {
        }
    }
}