using System.Text;
using BridgeBotNext.Providers.Vk;

namespace BridgeBotNext.Attachments
{
    public class PlaceAttachment : Attachment, IVkSpecialAttachment
    {
        public PlaceAttachment(double latitude, double longitude, string name = null, string address = null,
            object meta = null) : base(meta)
        {
            Latitude = latitude;
            Longitude = longitude;
            Name = name;
            Address = address;
        }

        public PlaceAttachment(double latitude, double longitude, object meta) : base(meta)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public double Latitude { get; }
        public double Longitude { get; }
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