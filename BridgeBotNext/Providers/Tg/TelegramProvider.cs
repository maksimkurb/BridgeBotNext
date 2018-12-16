using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BridgeBotNext.Attachments;
using Easy.Logger.Interfaces;
using HeyRed.Mime;
using MoreLinq;
using SkiaSharp;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using File = Telegram.Bot.Types.File;

namespace BridgeBotNext.Providers.Tg
{
    /// <inheritdoc />
    public class TelegramProvider : Provider
    {
        private readonly string _apiToken;
        private readonly TelegramMediaGroupSettler _mediaGroupSettler = new TelegramMediaGroupSettler();
        protected readonly TelegramBotClient BotClient;
        protected readonly IEasyLogger Logger = Logging.LogService.GetLogger<TelegramProvider>();

        public TelegramProvider(string apiToken)
        {
            _apiToken = apiToken;
            BotClient = new TelegramBotClient(apiToken);
            BotClient.OnMessage += _onMessage;

            _mediaGroupSettler.MediaGroupReceived += _onMediaGroupMessage;
        }

        public override string Name => "tg";
        public override string DisplayName => "Telegram";

        public override async Task Connect()
        {
            if (!BotClient.IsReceiving)
            {
                var me = await BotClient.GetMeAsync();
                Logger.DebugFormat("Telegram bot @{0} ({1} {2}) connected", me.Username, me.FirstName, me.LastName);
                BotClient.StartReceiving();
            }
        }

        public override async Task Disconnect()
        {
            if (BotClient.IsReceiving)
            {
                Logger.DebugFormat("Telegram bot stops receiving events");
                BotClient.StopReceiving();   
            }
        }

        public override string FormatSender(Person sender)
        {
            return $"💬 [{sender.DisplayName}]({sender.ProfileUrl}):";
        }

        public override async Task SendMessage(Conversation conversation, Message message)
        {
            var chat = new ChatId(Convert.ToInt64(conversation.Id));

            var fwdExpanded = message.ForwardedMessages?
                .ExpandWithLevel(el => el.ForwardedMessages);
            var fwd = fwdExpanded as (Message Item, int Level)[] ?? fwdExpanded?.ToArray();

            List<Attachment> attachments = new List<Attachment>();
            
            if (!fwd.IsNullOrEmpty())
            {
                attachments.AddRange(fwd
                    .Select(e => e.Item)
                    .Where(msg => !msg.Attachments.IsNullOrEmpty())
                    .SelectMany(msg => msg.Attachments)
                );
            }
            
            if (!message.Attachments.IsNullOrEmpty())
            {
                attachments.AddRange(message.Attachments);
            }

            
            if (attachments.Any())
            {
                Logger.Trace("Sending message with attachments");

                foreach (var at in attachments)
                {
                    if (at is AlbumAttachment album)
                    {
                        Func<IAlbumableAttachment, IAlbumInputMedia> AlbumAttachmentSelector()
                        {
                            return media =>
                            {
                                if (media is PhotoAttachment albumPhoto)
                                {
                                    var tgPhoto = new InputMediaPhoto(new InputMedia(albumPhoto.Url));
                                    tgPhoto.Caption = albumPhoto.Caption;
                                    return tgPhoto;
                                }

                                if (media is VideoAttachment albumVideo)
                                {
                                    var tgVideo = new InputMediaVideo(albumVideo.Url);
                                    tgVideo.Caption = albumVideo.Caption;
                                    return tgVideo;
                                }

                                return null;
                            };
                        }

                        var chunks = album.Media
                            .Select((val, i) => (val, i))
                            .GroupBy(tuple => tuple.i / 10);

                        foreach (var chunk in chunks)
                        {
                            await BotClient.SendMediaGroupAsync(chunk.Select(x => x.val).Select(AlbumAttachmentSelector()), chat);
                        }
                    }
                    else if (at is AnimationAttachment animation)
                        await BotClient.SendAnimationAsync(chat, _getInputFile(message, animation), animation.Duration,
                            animation.Width,
                            animation.Height, caption: animation.Caption);
                    else if (at is VoiceAttachment voice)
                    {
                        var req = (HttpWebRequest) WebRequest.Create(voice.Url);
                        req.Timeout = 15000;
                        var resp = (HttpWebResponse) req.GetResponse();
                        await BotClient.SendVoiceAsync(chat, new InputOnlineFile(resp.GetResponseStream(), voice.FileName), voice.Caption);
                    }
                    else if (at is AudioAttachment audio)
                        await BotClient.SendAudioAsync(chat, _getInputFile(message, audio), audio.Caption,
                            duration: audio.Duration,
                            performer: audio.Performer, title: audio.Title);
                    else if (at is ContactAttachment contact)
                        await BotClient.SendContactAsync(chat, contact.Phone, contact.FirstName, contact.LastName,
                            vCard: contact.VCard);
                    else if (at is LinkAttachment link)
                        await BotClient.SendTextMessageAsync(chat, link.Url);
                    else if (at is StickerAttachment sticker)
                    {
                        var inputFile = _getInputFile(message, sticker);
                        if (sticker.MimeType == "image/webp")
                        {
                            await BotClient.SendStickerAsync(chat, inputFile);
                        } else {
                            Logger.Trace("Converting sticker to webp format");
                            var req = (HttpWebRequest) WebRequest.Create(inputFile.Url);
                            req.Timeout = 15000;
                            var resp = (HttpWebResponse) req.GetResponse();
                            var image = SKImage.FromBitmap(SKBitmap.Decode(resp.GetResponseStream()));
                            using (var p = image.Encode(SKEncodedImageFormat.Webp, 100))
                            {
                                await BotClient.SendStickerAsync(chat,
                                    new InputMedia(p.AsStream(), "sticker.webp"));
                            }
                        }
                    }
                    else if (at is PhotoAttachment photo)
                        await BotClient.SendPhotoAsync(chat, _getInputFile(message, photo), photo.Caption);
                    else if (at is PlaceAttachment place)
                    {
                        if (place.Name != null || place.Address != null)
                            await BotClient.SendVenueAsync(chat, place.Latitude, place.Longitude, place.Name,
                                place.Address);
                        else
                            await BotClient.SendLocationAsync(chat, place.Latitude, place.Longitude);
                    }
                    else if (at is VideoAttachment video)
                        await BotClient.SendVideoAsync(chat, _getInputFile(message, video), video.Duration,
                            video.Width,
                            video.Height,
                            video.Caption);
                    else if (at is FileAttachment file)
                    {
                        if (file.MimeType == "image/gif" || file.MimeType == "application/pdf" || file.MimeType == "application/zip")
                            await BotClient.SendDocumentAsync(chat, _getInputFile(message, file), file.Caption);
                        else
                        {
                            var req = (HttpWebRequest) WebRequest.Create(file.Url);
                            req.Timeout = 15000;
                            var resp = (HttpWebResponse) req.GetResponse();
                            await BotClient.SendDocumentAsync(chat, new InputOnlineFile(resp.GetResponseStream(), file.FileName), file.Caption);
                        }
                    }
                }
            }

            var body = new StringBuilder();

            if (message.OriginSender != null)
            {
                body.AppendLine(FormatSender(message.OriginSender));
            }
            
            if (!fwd.IsNullOrEmpty())
            {
                body.AppendLine(FormatForwardedMessages(fwd));
            }

            if (!string.IsNullOrEmpty(message.Body))
            {
                body.AppendLine(message.Body);
            }
            await BotClient.SendTextMessageAsync(new ChatId(conversation.Id), body.ToString(), ParseMode.Markdown);
        }

        private InputOnlineFile _getInputFile(Message message, Attachment attachment)
        {
            if (message.OriginSender != null && message.OriginSender.Provider == this)
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
            return new Conversation(this, tgChat.Id.ToString(), tgChat.Title);
        }

        private TelegramPerson _extractPerson(User tgUser)
        {
            var fullName = new StringBuilder().Append(tgUser.FirstName);
            if (tgUser.LastName != null)
                fullName.AppendFormat(" {0}", tgUser.LastName);
            return new TelegramPerson(this, tgUser.Username, fullName.ToString());
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
                var photo = tgMessage.Photo.MaxBy(ph => ph.Width);
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

        private async Task<Message> _extractMessage(Telegram.Bot.Types.Message tgMessage, bool ignoreForwarded = true)
        {
            var forwarded = await _extractForwarded(tgMessage);
            var conversation = _extractConversation(tgMessage.Chat);
            Person person = _extractPerson(tgMessage.From);
            
            if (ignoreForwarded && tgMessage.ForwardFrom != null)
            {
                return new Message(conversation, person, forwardedMessages: forwarded);
            }
            
            var attachment = await _extractAttachment(tgMessage);
            return new Message(conversation, person, tgMessage.Text, forwarded, new[] {attachment});
        }

        private async Task<IEnumerable<Message>> _extractForwarded(Telegram.Bot.Types.Message tgMessage)
        {
            if (tgMessage.ReplyToMessage != null)
            {
                return new Message[]
                {
                    await _extractMessage(tgMessage.ReplyToMessage)
                };
            } else if (tgMessage.ForwardFrom != null)
            {
                return new Message[]
                {
                    await _extractMessage(tgMessage.ReplyToMessage, false)
                };
            }

            return null;
        }

        private async void _onMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var message = await _extractMessage(e.Message);

            if (e.Message.MediaGroupId != null && message.Attachments.Any())
            {
                _mediaGroupSettler.AddMediaGroupMessage(e.Message.MediaGroupId, message, message.Attachments.First());
                return;
            }

            if (message.Body.StartsWith("/"))
            {
                OnCommandReceived(new MessageEventArgs(message));
                return;
            }

            OnMessageReceived(new MessageEventArgs(message));
        }

        private void _onMediaGroupMessage(object sender, TelegramMediaGroupSettler.MediaGroupEventArgs e)
        {
            var oldMessage = e.Message;

            var albumAttachments = e.Attachments.OfType<IAlbumableAttachment>();
            var otherAttachments = e.Attachments.Where(at => !(at is IAlbumableAttachment)).ToArray();

            var attachments = new Attachment[otherAttachments.Length + 1];
            attachments[0] = new AlbumAttachment(null, albumAttachments);
            otherAttachments.CopyTo(attachments, 1);

            var message = new Message(oldMessage.OriginConversation, oldMessage.OriginSender, oldMessage.Body,
                oldMessage.ForwardedMessages, attachments);
            OnMessageReceived(new MessageEventArgs(message));
        }
    }
}