using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BridgeBotNext.Attachments;
using Easy.Logger.Interfaces;
using HeyRed.Mime;
using MixERP.Net.VCards.Models;
using MoreLinq;
using Newtonsoft.Json.Linq;
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
        private IEasyLogger _logger = Logging.LogService.GetLogger<TelegramProvider>();
        private TelegramBotClient _botClient { get; }
        private string _apiToken { get; }
        private TelegramMediaGroupSettler _mediaGroupSettler { get; } = new TelegramMediaGroupSettler();
        public override string Name => "tg";
        public override string DisplayName => "Telegram";

        public TelegramProvider(string apiToken)
        {
            _apiToken = apiToken;
            _botClient = new TelegramBotClient(apiToken);
            _botClient.OnMessage += _onMessage;

            _mediaGroupSettler.MediaGroupReceived += _onMediaGroupMessage;
        }

        public override Task Connect()
        {
            return Task.Factory.StartNew(async () =>
            {
                var me = await _botClient.GetMeAsync();
                _logger.DebugFormat("Telegram bot @{0} ({1} {2}) connected", me.Username, me.FirstName, me.LastName);
                _botClient.StartReceiving();
            });
        }

        public override Task Disconnect()
        {
            return Task.Factory.StartNew(() =>
            {
                _logger.DebugFormat("Telegram bot stops receiving events");
                _botClient.StopReceiving();
            });
        }

        public override async Task SendMessage(Conversation conversation, Message message)
        {
            var chat = new ChatId(Convert.ToInt64(conversation.Id));
            if (message.Attachments.Any())
                _logger.Trace("Sending message with attachments");
            await Task.WhenAll(message.Attachments.Select(async at =>
            {
                if (at is AlbumAttachment album)
                    return _botClient.SendMediaGroupAsync(album.Media.Select<IAlbumAttachment, IAlbumInputMedia>(
                        media =>
                        {
                            if (media is PhotoAttachment photo)
                                return new InputMediaPhoto(new InputMedia(photo.Url));
                            else if (media is VideoAttachment video) return new InputMediaVideo(video.Url);

                            return null;
                        }), chat);
                else if (at is AnimationAttachment animation)
                    return _botClient.SendAnimationAsync(chat, _getInputFile(message, animation), animation.Duration,
                        animation.Width,
                        animation.Height, caption: animation.Caption);
                else if (at is VoiceAttachment voice)
                    return _botClient.SendVoiceAsync(chat, _getInputFile(message, voice), voice.Caption);
                else if (at is AudioAttachment audio)
                    return _botClient.SendAudioAsync(chat, _getInputFile(message, audio), audio.Caption,
                        duration: audio.Duration,
                        performer: audio.Performer, title: audio.Title);
                else if (at is ContactAttachment contact)
                    return _botClient.SendContactAsync(chat, contact.Phone, contact.FirstName, contact.LastName,
                        vCard: contact.VCard);
                else if (at is LinkAttachment link)
                    return _botClient.SendTextMessageAsync(chat, link.Url);
                else if (at is StickerAttachment sticker)
                {
                    var inputFile = _getInputFile(message, sticker);
                    if (inputFile.FileType == FileType.Url && sticker.MimeType != "image/webp")
                    {
                        _logger.Trace("Converting sticker to webp format");
                        using (MemoryStream stream = new MemoryStream())
                        {
                            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(inputFile.Url);
                            req.Timeout = 15000;
                            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                            var image = SKImage.FromBitmap(SKBitmap.Decode(resp.GetResponseStream()));
                            using (SKData p = image.Encode(SKEncodedImageFormat.Webp, 100))
                            {
                                return _botClient.SendStickerAsync(chat, new InputMedia(p.AsStream(), "sticker.webp"));
                            }
                        }
                    }
                    
                    return _botClient.SendStickerAsync(chat, inputFile);   
                }
                else if (at is PhotoAttachment photo)
                    return _botClient.SendPhotoAsync(chat, _getInputFile(message, photo), photo.Caption);
                else if (at is PlaceAttachment place)
                {
                    if (place.Name != null || place.Address != null)
                        return _botClient.SendVenueAsync(chat, place.Latitude, place.Longitude, place.Name,
                            place.Address);
                    else
                        return _botClient.SendLocationAsync(chat, place.Latitude, place.Longitude);
                }
                else if (at is VideoAttachment video)
                    return _botClient.SendVideoAsync(chat, _getInputFile(message, video), video.Duration, video.Width,
                        video.Height,
                        video.Caption);

                return Task.CompletedTask;
            }));

            if (!string.IsNullOrEmpty(message.Body))
            {
                // TODO: resend forwarded messages
                await _botClient.SendTextMessageAsync(new ChatId(conversation.Id), message.Body);
            }
        }

        private InputOnlineFile _getInputFile(Message message, Attachment attachment)
        {
            if (message.OriginSender.Provider == this)
            {
                if (attachment.Meta is Audio audio)
                    return audio.FileId;
                else if (attachment.Meta is Document document)
                    return document.FileId;
                else if (attachment.Meta is Animation animation)
                    return animation.FileId;
                else if (attachment.Meta is PhotoSize photo)
                    return photo.FileId;
                else if (attachment.Meta is Sticker sticker)
                    return sticker.FileId;
                else if (attachment.Meta is Video video)
                    return video.FileId;
                else if (attachment.Meta is Voice voice)
                    return voice.FileId;
                else if (attachment.Meta is VideoNote videoNote)
                    return videoNote.FileId;
            }

            return attachment.Url;
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
        private async Task<Attachment> _ExtractAttachment(Telegram.Bot.Types.Message tgMessage)
        {
            if (tgMessage.Audio != null)
            {
                var file = await _botClient.GetFileAsync(tgMessage.Audio.FileId);
                return new AudioAttachment(_getDownloadUrl(file), tgMessage.Audio, null, tgMessage.Caption, file.FileSize,
                    tgMessage.Audio.MimeType, tgMessage.Audio.Title, tgMessage.Audio.Performer,
                    tgMessage.Audio.Duration);
            }
            else if (tgMessage.Document != null)
            {
                var file = await _botClient.GetFileAsync(tgMessage.Document.FileId);
                return new FileAttachment(_getDownloadUrl(file), tgMessage.Document, tgMessage.Document.FileName,
                    tgMessage.Caption, file.FileSize, tgMessage.Document.MimeType);
            }
            else if (tgMessage.Animation != null)
            {
                var file = await _botClient.GetFileAsync(tgMessage.Animation.FileId);
                return new AnimationAttachment(_getDownloadUrl(file), tgMessage.Animation, tgMessage.Animation.FileName,
                    tgMessage.Caption, file.FileSize, tgMessage.Animation.MimeType, tgMessage.Animation.Duration,
                    tgMessage.Animation.Width, tgMessage.Animation.Height);
            }
            else if (tgMessage.Game != null)
            {
                throw new UnsupportedAttachmentException("game");
            }
            else if (tgMessage.Photo != null)
            {
                var photo = tgMessage.Photo.MaxBy(ph => ph.Width);
                var file = await _botClient.GetFileAsync(photo.FileId);
                return new PhotoAttachment(_getDownloadUrl(file), photo, tgMessage.Caption, null, file.FileSize,
                    MimeTypesMap.GetMimeType(file.FilePath));
            }
            else if (tgMessage.Sticker != null)
            {
                var file = await _botClient.GetFileAsync(tgMessage.Sticker.FileId);
                return new StickerAttachment(_getDownloadUrl(file), tgMessage.Sticker, null, file.FileSize, "image/webp");
            }
            else if (tgMessage.Video != null)
            {
                var file = await _botClient.GetFileAsync(tgMessage.Video.FileId);
                return new VideoAttachment(_getDownloadUrl(file), tgMessage.Video, tgMessage.Caption, null, file.FileSize,
                    tgMessage.Video.MimeType, tgMessage.Caption, tgMessage.Video.Duration, tgMessage.Video.Width,
                    tgMessage.Video.Height);
            }
            else if (tgMessage.Voice != null)
            {
                var file = await _botClient.GetFileAsync(tgMessage.Voice.FileId);
                return new VoiceAttachment(_getDownloadUrl(file), tgMessage.Voice, tgMessage.Caption, null, file.FileSize,
                    tgMessage.Voice.MimeType, tgMessage.Caption, tgMessage.Voice.Duration);
            }
            else if (tgMessage.VideoNote != null)
            {
                // TODO: Mark video note somehow
                var file = await _botClient.GetFileAsync(tgMessage.VideoNote.FileId);
                return new VideoAttachment(_getDownloadUrl(file), tgMessage.VideoNote, tgMessage.Caption, null, file.FileSize,
                    "video/mp4", tgMessage.Caption, tgMessage.VideoNote.Duration, tgMessage.VideoNote.Length,
                    tgMessage.VideoNote.Length);
            }
            else if (tgMessage.Contact != null)
            {
                return new ContactAttachment(tgMessage.Contact.Vcard, tgMessage.Contact);
            }
            else if (tgMessage.Location != null)
            {
                return new PlaceAttachment(tgMessage.Location.Latitude, tgMessage.Location.Longitude,
                    tgMessage.Location);
            }
            else if (tgMessage.Venue != null)
            {
                return new PlaceAttachment(tgMessage.Venue.Location.Latitude, tgMessage.Venue.Location.Longitude,
                    tgMessage.Venue.Title, tgMessage.Venue.Address, tgMessage.Venue);
            }

            return null;
        }

        private string _getDownloadUrl(File file)
        {
            return $"https://api.telegram.org/file/bot{_apiToken}/{file.FilePath}";
        }

        private async Task<Message> _extractMessage(Telegram.Bot.Types.Message tgMessage)
        {
            var conversation = _extractConversation(tgMessage.Chat);
            Person person = _extractPerson(tgMessage.From);
            var attachment = await _ExtractAttachment(tgMessage);
            return new Message(conversation, person, tgMessage.Text, null, new []{ attachment });
        }

        private async void _onMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var message = await _extractMessage(e.Message);
            if (e.Message.MediaGroupId != null && message.Attachments.Length > 0)
            {
                _mediaGroupSettler.AddMediaGroupMessage(e.Message.MediaGroupId, message, message.Attachments[0]);
                return;
            }

            OnMessageReceived(new MessageEventArgs(message));
        }

        private void _onMediaGroupMessage(object sender, TelegramMediaGroupSettler.MediaGroupEventArgs e)
        {
            var oldMessage = e.Message;

            var albumAttachments = e.Attachments.OfType<IAlbumAttachment>();
            var otherAttachments = e.Attachments.Where(at => !(at is IAlbumAttachment)).ToArray();
            
            var attachments = new Attachment[otherAttachments.Length + 1];
            attachments[0] = new AlbumAttachment(null, albumAttachments);
            otherAttachments.CopyTo(attachments, 1);
            
            var message = new Message(oldMessage.OriginConversation, oldMessage.OriginSender, oldMessage.Body,
                oldMessage.ForwardedMessages, attachments);
            OnMessageReceived(new MessageEventArgs(message));
        }
    }
}