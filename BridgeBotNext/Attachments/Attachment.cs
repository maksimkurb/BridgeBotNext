namespace BridgeBotNext.Attachments
{
    public abstract class Attachment
    {
        protected Attachment(object meta)
        {
            Meta = meta;
        }

        protected Attachment(string url, object meta)
        {
            Url = url;
            Meta = meta;
        }


        /**
         * Attachment url
         */
        public virtual string Url { get; }

        /**
         * Meta information from provider
         * Can be used to store some IDs and reuse them when attaching some file within one provider 
         */
        public object Meta { get; }

        /**
         * Converts attachment to readable string
         * e.g. audio URL or link to a maps
         */
        public new abstract string ToString();
    }
}