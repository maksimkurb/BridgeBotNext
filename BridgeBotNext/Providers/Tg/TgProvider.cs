using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using BridgeBotNext.Attachments;
using BridgeBotNext.Configuration;
using BridgeBotNext.Entities;

using HeyRed.Mime;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MoreLinq;

using SkiaSharp;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

using Message = BridgeBotNext.Entities.Message;

namespace BridgeBotNext.Providers.Tg
{
    /// <inheritdoc />
    public class TgProvider : Provider
    {
        private readonly string _apiToken;
        private readonly TgMediaGroupSettler _mediaGroupSettler = new TgMediaGroupSettler();
        protected readonly TelegramBotClient BotClient;
        protected readonly ILogger<TgProvider> Logger;
        protected string BotUserName;

        public TgProvider(ILogger<TgProvider> logger, IOptions<TgConfiguration> configuration)
        {
            _apiToken = configuration.Value.BotToken;
            BotClient = new TelegramBotClient(_apiToken);
            BotClient.OnMessage += _onMessage;

            Logger = logger;

            _mediaGroupSettler.MediaGroupReceived += _onMediaGroupMessage;
        }

        public override string Name => "tg";
        public override string DisplayName => "Telegram";

        public override async Task Connect()
        {
            if (!BotClient.IsReceiving)
            {
                var me = await BotClient.GetMeAsync();
                BotUserName = me.Username;
                Logger.LogDebug("Telegram bot @{0} ({1} {2}) connected", me.Username, me.FirstName, me.LastName);
                BotClient.StartReceiving();
            }
        }

        public override void Dispose()
        {
            if (BotClient.IsReceiving)
            {
                Logger.LogDebug("Telegram bot stops receiving events");
                BotClient.StopReceiving();
            }
        }

        protected override string FormatSender(Person sender)
        {
            return $"💬 <a href=\"{sender.ProfileUrl}\">{sender.DisplayName}</a>:";
        }

        protected override string SanitizeMessageBody(string body)
        {
            return body.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        public override async Task SendMessage(Conversation conversation, Message message)
        {
            Logger.LogTrace("Send message to conversation {0}", conversation.OriginId);
            var chat = new ChatId(Convert.ToInt64(conversation.OriginId));

            #region Get forwarded and attachments

            var fwd = FlattenForwardedMessages(message);
            var attachments = GetAllAttachments(message, fwd);

            #endregion

            #region Send message

            var body = FormatMessageBody(message, fwd);
            if (body.Length > 0)
                await BotClient.SendTextMessageAsync(new ChatId(conversation.OriginId), body, ParseMode.Html, true);

            #endregion

            #region Send attachments

            if (attachments.Any())
            {
                Logger.LogTrace("Sending message with attachments");

                var groupableAttachments = attachments.OfType<ITgGroupableAttachment>();

                Func<ITgGroupableAttachment, IAlbumInputMedia> AlbumAttachmentSelector()
                {
                    return media =>
                    {
                        switch (media)
                        {
                            case PhotoAttachment albumPhoto:
                                {
                                    if (albumPhoto.Meta is PhotoSize photo)
                                        return new InputMediaPhoto(new InputMedia(photo.FileId));
                                    var tgPhoto = new InputMediaPhoto(new InputMedia(albumPhoto.Url));
                                    tgPhoto.Caption = albumPhoto.Caption;
                                    return tgPhoto;
                                }
                            case VideoAttachment albumVideo:
                                {
                                    if (albumVideo.Meta is Video video)
                                        return new InputMediaPhoto(new InputMedia(video.FileId));
                                    var tgVideo = new InputMediaVideo(albumVideo.Url);
                                    tgVideo.Caption = albumVideo.Caption;
                                    tgVideo.Duration = albumVideo.Duration ?? 0;
                                    tgVideo.Width = albumVideo.Width;
                                    tgVideo.Height = albumVideo.Height;
                                    return tgVideo;
                                }
                            default:
                                return null;
                        }
                    };
                }

                var chunks = groupableAttachments
                    .Select((val, i) => (val, i))
                    .GroupBy(tuple => tuple.i / 10);

                foreach (var chunk in chunks)
                    await BotClient.SendMediaGroupAsync(
                        chunk.Select(x => x.val).Select(AlbumAttachmentSelector()), chat);

                var restAttachments = attachments.Where(at => !(at is ITgGroupableAttachment));
                foreach (var at in restAttachments)
                    if (at is AnimationAttachment animation)
                    {
                        await BotClient.SendAnimationAsync(chat, _getInputFile(message, animation),
                            animation.Duration ?? 0,
                            animation.Width,
                            animation.Height, caption: animation.Caption);
                    }
                    else if (at is VoiceAttachment voice)
                    {
                        var req = (HttpWebRequest)WebRequest.Create(voice.Url);
                        req.Timeout = 15000;
                        var resp = (HttpWebResponse)req.GetResponse();
                        await BotClient.SendVoiceAsync(chat,
                            new InputOnlineFile(resp.GetResponseStream(), voice.FileName), voice.Caption);
                    }
                    else if (at is AudioAttachment audio)
                    {
                        await BotClient.SendAudioAsync(chat, _getInputFile(message, audio), audio.Caption,
                            duration: audio.Duration ?? 0,
                            performer: audio.Performer, title: audio.Title);
                    }
                    else if (at is ContactAttachment contact)
                    {
                        await BotClient.SendContactAsync(chat, contact.Phone, contact.FirstName, contact.LastName,
                            vCard: contact.VCard);
                    }
                    else if (at is LinkAttachment link)
                    {
                        await BotClient.SendTextMessageAsync(chat, link.Url);
                    }
                    else if (at is StickerAttachment sticker)
                    {
                        var inputFile = _getInputFile(message, sticker);
                        if (sticker.MimeType == "image/webp")
                        {
                            await BotClient.SendStickerAsync(chat, inputFile);
                        }
                        else
                        {
                            Logger.LogTrace("Converting sticker to webp format");
                            var req = (HttpWebRequest)WebRequest.Create(inputFile.Url);
                            req.Timeout = 15000;
                            var resp = (HttpWebResponse)req.GetResponse();
                            var image = SKImage.FromBitmap(SKBitmap.Decode(resp.GetResponseStream()));
                            using (var p = image.Encode(SKEncodedImageFormat.Webp, 100))
                            {
                                await BotClient.SendStickerAsync(chat,
                                    new InputMedia(p.AsStream(), "sticker.webp"));
                            }
                        }
                    }
                    else if (at is PlaceAttachment place)
                    {
                        if (place.Name != null && place.Address != null)
                            await BotClient.SendVenueAsync(chat, (float)place.Latitude, (float)place.Longitude,
                                place.Name,
                                place.Address);
                        else
                            await BotClient.SendLocationAsync(chat, (float)place.Latitude, (float)place.Longitude);
                    }
                    else if (at is FileAttachment file)
                    {
                        if (file.MimeType == "image/gif" || file.MimeType == "application/pdf" ||
                            file.MimeType == "application/zip")
                        {
                            await BotClient.SendDocumentAsync(chat, _getInputFile(message, file), file.Caption);
                        }
                        else
                        {
                            var req = (HttpWebRequest)WebRequest.Create(file.Url);
                            req.Timeout = 15000;
                            var resp = (HttpWebResponse)req.GetResponse();
                            await BotClient.SendDocumentAsync(chat,
                                new InputOnlineFile(resp.GetResponseStream(), file.FileName), file.Caption);
                        }
                    }
            }

            #endregion
        }

        private InputOnlineFile _getInputFile(Message message, Attachment attachment)
        {
            if (message.OriginSender != null && message.OriginSender.Provider.Equals(this))
            {
                if (attachment.Meta is Audio audio)
                    return audio.FileId;
                if (attachment.Meta is Document document)
                    return document.FileId;
                if (attachment.Meta is Animation animation)
                    return animation.FileId;
                if (attachment.Meta is PhotoSize photo)
                    return photo.FileId;
                if (attachment.Meta is Sticker sticker)
                    return sticker.FileId;
                if (attachment.Meta is Video video)
                    return video.FileId;
                if (attachment.Meta is Voice voice)
                    return voice.FileId;
                if (attachment.Meta is VideoNote videoNote)
                    return videoNote.FileId;
            }

            return new InputOnlineFile(attachment.Url);
        }

        private Conversation _extractConversation(Chat tgChat)
        {
            return new Conversation(this, tgChat.Id.ToString(), tgChat.Title ?? tgChat.Username ?? $"#{tgChat.Id}");
        }

        private TgPerson _extractPerson(User tgUser)
        {
            var fullName = new StringBuilder().Append(tgUser.FirstName);
            if (tgUser.LastName != null)
                fullName.AppendFormat(" {0}", tgUser.LastName);
            return new TgPerson(this, tgUser.Id, tgUser.Username, fullName.ToString());
        }

        /**
         * Extracts attachment from the message
         */
        private async Task<Attachment> _extractAttachment(Telegram.Bot.Types.Message tgMessage)
        {
            if (tgMessage.Audio != null)
            {
                var file = await BotClient.GetFileAsync(tgMessage.Audio.FileId);
                return new AudioAttachment(_getDownloadUrl(file), tgMessage.Audio, tgMessage.Caption,
                    fileSize: file.FileSize,
                    mimeType: tgMessage.Audio.MimeType, title: tgMessage.Audio.Title,
                    performer: tgMessage.Audio.Performer,
                    duration: tgMessage.Audio.Duration);
            }

            if (tgMessage.Document != null)
            {
                var file = await BotClient.GetFileAsync(tgMessage.Document.FileId);
                return new FileAttachment(_getDownloadUrl(file), tgMessage.Document,
                    tgMessage.Caption, tgMessage.Document.FileName, file.FileSize, tgMessage.Document.MimeType);
            }

            if (tgMessage.Animation != null)
            {
                var file = await BotClient.GetFileAsync(tgMessage.Animation.FileId);
                return new AnimationAttachment(_getDownloadUrl(file), tgMessage.Animation, tgMessage.Caption,
                    tgMessage.Animation.FileName, file.FileSize, tgMessage.Animation.MimeType,
                    tgMessage.Animation.Duration,
                    tgMessage.Animation.Width, tgMessage.Animation.Height);
            }

            if (tgMessage.Game != null) throw new UnsupportedAttachmentException("game");

            if (tgMessage.Photo != null)
            {
                var photo = tgMessage.Photo.MaxBy(ph => ph.Width).First();
                var file = await BotClient.GetFileAsync(photo.FileId);
                return new PhotoAttachment(_getDownloadUrl(file), photo, tgMessage.Caption, fileSize: file.FileSize,
                    mimeType: MimeTypesMap.GetMimeType(file.FilePath));
            }

            if (tgMessage.Sticker != null)
            {
                var file = await BotClient.GetFileAsync(tgMessage.Sticker.FileId);
                return new StickerAttachment(_getDownloadUrl(file), tgMessage.Sticker, fileSize: file.FileSize,
                    mimeType: "image/webp");
            }

            if (tgMessage.Video != null)
            {
                var file = await BotClient.GetFileAsync(tgMessage.Video.FileId);
                return new VideoAttachment(_getDownloadUrl(file), tgMessage.Video, tgMessage.Caption,
                    fileSize: file.FileSize,
                    mimeType: tgMessage.Video.MimeType, duration: tgMessage.Video.Duration,
                    width: tgMessage.Video.Width,
                    height: tgMessage.Video.Height);
            }

            if (tgMessage.Voice != null)
            {
                var file = await BotClient.GetFileAsync(tgMessage.Voice.FileId);
                return new VoiceAttachment(_getDownloadUrl(file), tgMessage.Voice, tgMessage.Caption,
                    fileSize: file.FileSize,
                    mimeType: tgMessage.Voice.MimeType, duration: tgMessage.Voice.Duration);
            }

            if (tgMessage.VideoNote != null)
            {
                // TODO: Mark video note somehow
                var file = await BotClient.GetFileAsync(tgMessage.VideoNote.FileId);
                return new VideoAttachment(_getDownloadUrl(file), tgMessage.VideoNote, tgMessage.Caption,
                    fileSize: file.FileSize,
                    mimeType: "video/mp4", duration: tgMessage.VideoNote.Duration, width: tgMessage.VideoNote.Length,
                    height: tgMessage.VideoNote.Length);
            }

            if (tgMessage.Contact != null)
                return new ContactAttachment(tgMessage.Contact.Vcard, tgMessage.Contact);
            if (tgMessage.Location != null)
                return new PlaceAttachment(tgMessage.Location.Latitude, tgMessage.Location.Longitude,
                    tgMessage.Location);
            if (tgMessage.Venue != null)
                return new PlaceAttachment(tgMessage.Venue.Location.Latitude, tgMessage.Venue.Location.Longitude,
                    tgMessage.Venue.Title, tgMessage.Venue.Address, tgMessage.Venue);

            return null;
        }

        private string _getDownloadUrl(File file)
        {
            return $"https://api.telegram.org/file/bot{_apiToken}/{file.FilePath}";
        }

        private async Task<Message> _extractMessage(Telegram.Bot.Types.Message tgMessage)
        {
            Logger.LogTrace("Message received");
            var conversation = _extractConversation(tgMessage.Chat);
            var person = _extractPerson(tgMessage.From);

            var attachments = new List<Attachment>();
            var at = await _extractAttachment(tgMessage);
            if (at != null) attachments.Add(at);

            if (tgMessage.Entities != null)
                attachments.AddRange(from entity in tgMessage.Entities
                                     where entity.Type == MessageEntityType.TextLink
                                     select new LinkAttachment(entity.Url));

            // Just forwarded message
            if (tgMessage.ForwardFrom != null)
            {
                var fwdPerson = _extractPerson(tgMessage.ForwardFrom);
                var fwdMessage = new Message(conversation, fwdPerson, tgMessage.Text, attachments: attachments);
                return new Message(conversation, person, forwardedMessages: new[] { fwdMessage });
            }

            // Reply to
            Message[] forwarded = null;
            if (tgMessage.ReplyToMessage != null)
            {
                forwarded = new[]
                {
                    await _extractMessage(tgMessage.ReplyToMessage)
                };
            }

            var text = tgMessage.Text ?? tgMessage.Caption;
            // Remove bot name from command
            if (!string.IsNullOrEmpty(text) && text.StartsWith("/") && !string.IsNullOrEmpty(BotUserName))
                text = text.Split($"@{BotUserName}", 2)[0];

            return new Message(conversation, person, text, forwarded, attachments);
        }

        private async void _onMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var message = await _extractMessage(e.Message);

            if (e.Message.MediaGroupId != null && message.Attachments.Any())
            {
                _mediaGroupSettler.AddMediaGroupMessage(e.Message.MediaGroupId, message, message.Attachments.First());
                return;
            }

            if (!string.IsNullOrEmpty(message.Body) && message.Body.StartsWith("/"))
            {
                OnCommandReceived(new MessageEventArgs(message));
                return;
            }

            OnMessageReceived(new MessageEventArgs(message));
        }

        private void _onMediaGroupMessage(object sender, TgMediaGroupSettler.MediaGroupEventArgs e)
        {
            var oldMessage = e.Message;

            var message = new Message(oldMessage.OriginConversation, oldMessage.OriginSender, oldMessage.Body,
                oldMessage.ForwardedMessages, e.Attachments);
            OnMessageReceived(new MessageEventArgs(message));
        }
    }
}