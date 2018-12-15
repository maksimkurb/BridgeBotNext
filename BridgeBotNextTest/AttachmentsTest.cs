using System.Collections.Generic;
using BridgeBotNext.Attachments;
using Xunit;

namespace BridgeBotNextTest
{
    public class AttachmentsTest
    {
        [Theory]
        [InlineData("http://site.com/file.jpg", "image.jpg", null, "image.jpg", "image/jpeg")]
        [InlineData("http://site.com/", null, "image/jpeg", "noname.jpeg", "image/jpeg")]
        [InlineData("http://site.com/file.jpg", null, null, "file.jpg", "image/jpeg")]
        [InlineData("http://site.com/", null, null, "noname", "application/octet-stream")]
        [InlineData("http://site.com/file", "file", null, "file", "application/octet-stream")]
        public void FileAttachmentNameAndMimeTypes(string url, string fileName, string mimeType,
            string expectedFileName, string expectedMimeType)
        {
            var at = new FileAttachment(url, fileName: fileName, mimeType: mimeType);
            Assert.Equal(expectedFileName, at.FileName);
            Assert.Equal(expectedMimeType, at.MimeType);
        }

        [Fact]
        public void FileAttachmentProperties()
        {
            var meta = new List<string>();
            var caption = "TEST_CAPTION";
            var url = "http://ya.ru";
            var at = new FileAttachment(url, meta, caption, "image.jpg");

            Assert.Equal(caption, at.Caption);

            Assert.Equal(url, at.Url);

            Assert.Same(meta, at.Meta);
            meta.Add("test");
            Assert.Equal(meta.Count, ((List<string>) at.Meta).Count);
        }
    }
}