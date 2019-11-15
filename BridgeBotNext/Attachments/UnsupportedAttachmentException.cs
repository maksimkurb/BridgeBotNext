using System;

namespace BridgeBotNext.Attachments
{
    public class UnsupportedAttachmentException : Exception
    {
        public UnsupportedAttachmentException()
        {
        }

        public UnsupportedAttachmentException(string attachmentType) : base($"Unsupported attachment: {attachmentType}")
        {
        }

        public override string Message { get; } = "Unsupported attachment";
    }
}