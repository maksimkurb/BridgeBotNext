using System;
using System.Linq;
using System.Text;
using MixERP.Net.VCards;
using MixERP.Net.VCards.Models;
using MixERP.Net.VCards.Serializer;
using MixERP.Net.VCards.Types;

namespace BridgeBotNext.Attachments
{
    public class ContactAttachment : FileAttachment
    {
        public ContactAttachment(string vCard, object meta = null) : base(meta)
        {
            var vcards = Deserializer.GetVCards(vCard);
            var vCards = vcards as VCard[] ?? vcards.ToArray();
            if (!vCards.Any()) throw new FormatException("VCard format is invalid");

            FirstName = vCards[0].FirstName;
            LastName = vCards[0].LastName;
            if (vCards[0].Telephones.Any()) Phone = vCards[0].Telephones.ElementAt(0).Number;

            if (vCards[0].Emails.Any()) Email = vCards[0].Emails.ElementAt(0).EmailAddress;

            VCard = vCard;
        }

        public ContactAttachment(string firstName, string lastName, string phone, string email, object meta = null) :
            base(meta)
        {
            FirstName = firstName;
            LastName = lastName;
            Phone = phone;
            Email = email;

            var generatedVCard = new VCard
            {
                Version = VCardVersion.V4,
                FormattedName = $"{firstName} {lastName}",
                FirstName = firstName,
                LastName = lastName
            };

            if (string.IsNullOrEmpty(phone)) generatedVCard.Telephones.Append(new Telephone {Number = phone});

            if (string.IsNullOrEmpty(email)) generatedVCard.Emails.Append(new Email {EmailAddress = email});

            VCard = generatedVCard.Serialize();
        }

        public string VCard { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string Phone { get; }
        public string Email { get; }

        /**
         * Contains base64 encoded VCard DataURL
         */
        public override string Url => $"data:text/vcard;base64,{Convert.ToBase64String(Encoding.UTF8.GetBytes(VCard))}";

        public override long FileSize => VCard.Length;
        public override string MimeType => "text/vcf";
    }
}