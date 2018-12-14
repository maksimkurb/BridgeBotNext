using System.Text;

namespace BridgeBotNext.Attachments
{
    public class PlaceAttachment : Attachment
    {
        public PlaceAttachment(float latitude, float longitude, string name = null, string address = null,
            object meta = null) : base(meta)
        {
            Latitude = latitude;
            Longitude = longitude;
            Name = name;
            Address = address;
        }

        public PlaceAttachment(float latitude, float longitude, object meta) : base(meta)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public float Latitude { get; }
        public float Longitude { get; }
        public string Name { get; }
        public string Address { get; }

        /**
         * Returns URL to Google Maps
         */
        public string GetUrl()
        {
            return $"https://maps.google.com/maps?t=m&q=loc:{Latitude:0.########}+{Longitude:0.########}";
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (Name != null) sb.AppendFormat("{0} ", Name);

            if (Address != null) sb.AppendFormat("({0}) ", Address);

            sb.Append(GetUrl());

            return sb.ToString();
        }
    }
}