using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BridgeBotNext.Attachments;
using BridgeBotNext.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MoreLinq;
using VkBotFramework;
using VkNet;
using VkNet.Abstractions;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using Attachment = BridgeBotNext.Attachments.Attachment;

namespace BridgeBotNext.Providers.Vk
{
    public class VkProvider : Provider
    {
        private readonly string _accessToken;
        protected readonly IVkApi ApiClient;
        protected readonly VkBot VkBot;
        private ulong _groupId;
        protected ILogger<VkProvider> Logger;

        public VkProvider(ILogger<VkProvider> logger, ILogger<VkBot> botLogger, ILogger<VkApi> apiLogger,
            IOptions<VkConfiguration> configuration)
        {
            _accessToken = configuration.Value.AccessToken;
            _groupId = configuration.Value.GroupId;
            Logger = logger;
            VkBot = new VkBot(_accessToken, (int) _groupId, botLogger);
            ApiClient = new VkApi(apiLogger);
            ApiClient.Authorize(new ApiAuthParams
            {
                AccessToken = _accessToken,
            });

            VkBot.OnMessageReceived += _onMessage;
        }

        public override string Name => "vk";
        public override string DisplayName => "Vkontakte";

        private async Task<Message> _extractMessage(VkNet.Model.Message msg)
        {
            var conversation = _extractConversation(msg);
            var person = _extractPerson(msg);
            var body = msg.Text ?? "";
            var attachments = _extractAttachments(msg);
            return new Message(conversation, person, body, attachments: attachments);
        }

        private List<Attachment> _extractAttachments(VkNet.Model.Message msg)
        {
            if (msg.Attachments.IsNullOrEmpty())
            {
                return null;
            }

            List<Attachment> attachments = new List<Attachment>();

            foreach (var at in msg.Attachments)
            {
                if (at.Instance is Photo photo)
                {
                    var maxPhoto = photo.Sizes.MaxBy(p => p.Width);
                    attachments.Add(new PhotoAttachment(maxPhoto.Url.ToString(), photo, photo.Text,
                        mimeType: "image/jpeg"));
                }
                else if (at.Instance is Video video)
                {
                    attachments.Add(new LinkAttachment(video.Player.ToString(), video));
                }
                else if (at.Instance is Audio audio)
                {
                    attachments.Add(new AudioAttachment(audio.Url.ToString(), audio, title: audio.Title,
                        performer: audio.Artist, duration: audio.Duration));
                }
                else if (at.Instance is Document doc)
                {
                    attachments.Add(new FileAttachment(doc.Uri, doc, doc.Title, fileSize: doc.Size));
                }
                else if (at.Instance is Link link)
                {
                    attachments.Add(new LinkAttachment(link.Uri.ToString(), link));
                }
                else if (at.Instance is Market market)
                {
                    attachments.Add(new LinkAttachment(
                        $"https://vk.com/market{market.OwnerId}?w=product{market.OwnerId}_{market.Id}", market));
                }
                else if (at.Instance is MarketAlbum marketAlbum)
                {
                    attachments.Add(new LinkAttachment(
                        $"https://vk.com/market{marketAlbum.OwnerId}?section=album_{marketAlbum.Id}", marketAlbum));
                }
                else if (at.Instance is Wall wall)
                {
                    attachments.Add(new LinkAttachment($"https://vk.com/wall{wall.OwnerId}_{wall.Id}", wall));
                }
                else if (at.Instance is Sticker sticker)
                {
                    var maxSticker = sticker.Images.MaxBy(s => s.Width);
                    attachments.Add(new StickerAttachment(maxSticker.Url.ToString(), sticker));
                }
                else if (at.Instance is Gift gift)
                {
                    var giftUri = gift.Thumb256 ?? gift.Thumb96 ?? gift.Thumb48;
                    attachments.Add(new PhotoAttachment(giftUri.ToString(), gift, "<gift>"));
                }
            }

            return attachments;
        }

        private Person _extractPerson(VkNet.Model.Message msg)
        {
            return new VkPerson(this, msg.FromId.ToString(), "user_name");
        }

        private Conversation _extractConversation(VkNet.Model.Message msg)
        {
            return new Conversation(this, msg.PeerId.ToString(), msg.Title);
        }

        private async void _onMessage(object sender, VkBot.MessageReceivedEventArgs e)
        {
            Logger.LogTrace("Message received");
            var message = await _extractMessage(e.message);

            if (message.Body != null && message.Body.StartsWith("/"))
            {
                OnCommandReceived(new MessageEventArgs(message));
                return;
            }

            OnMessageReceived(new MessageEventArgs(message));
        }


        public override Task Connect()
        {
            Task.Run(VkBot.StartAsync);
            return Task.CompletedTask;
        }

        public override Task Disconnect()
        {
            VkBot.Dispose();
            return Task.CompletedTask;
        }

        private async Task<byte[]> _downloadFile(string url)
        {
            if (url.StartsWith("data:"))
            {
                var parts = url.Split(',', 2);
                if (parts.Length != 2)
                {
                    throw new FormatException("Data URL does not contain separator");
                }

                return Convert.FromBase64String(parts[1]);
            }

            var wc = new WebClient();
            return await wc.DownloadDataTaskAsync(new Uri(url));
        }

        private async Task<string> _uploadFile(FileAttachment at, UploadServerInfo server,
            string overrideFileName = null, string overrideMimeType = null)
        {
            var file = await _downloadFile(at.Url);

            var postParameters = new Dictionary<string, object>
            {
                {
                    "file",
                    new FormUpload.FileParameter(file, overrideFileName ?? at.FileName, overrideMimeType ?? at.MimeType)
                }
            };

            using (var webResponse =
                FormUpload.MultipartFormDataPost(server.UploadUrl, "MeBridgeBot/2", postParameters))
            {
                var responseReader = new StreamReader(webResponse.GetResponseStream() ??
                                                      throw new NullReferenceException("Response stream is null"));
                var fullResponse = responseReader.ReadToEnd();
                return fullResponse;
            }
        }

        private async Task<MediaAttachment> _uploadDocument(FileAttachment at, UploadServerInfo server,
            string overrideFileName = null, string overrideMimeType = null)
        {
            try
            {
                var file = await _uploadFile(at, server, overrideFileName, overrideMimeType);
                var document = await ApiClient.Docs.SaveAsync(file, at.FileName);
                return document[0];
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to upload document to vk server");
            }

            return null;
        }

        private async Task<MediaAttachment> _uploadPhoto(PhotoAttachment ph, UploadServerInfo server)
        {
            try
            {
                var file = await _uploadFile(ph, server);
                var photo = await ApiClient.Photo.SaveMessagesPhotoAsync(file);
                return photo[0];
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to upload photo to vk server");
            }

            return null;
        }

        public override async Task SendMessage(Conversation conversation, Message message)
        {
            Logger.LogTrace("Send message to conversation {0}", conversation.Id);
            var peerId = Convert.ToInt32(conversation.Id);

            #region Get forwarded and attachments

            var fwd = FlattenForwardedMessages(message);
            var attachments = GetAllAttachments(message, fwd);

            #endregion


            if (attachments.Any())
            {
                Logger.LogTrace("Sending message with attachments");
                try
                {
                    #region Get upload server urls

                    UploadServerInfo photoUploadServer = null;
                    UploadServerInfo docsUploadServer = null;
                    UploadServerInfo voiceUploadServer = null;
                    await Task.WhenAll(Task.Run(async () =>
                    {
                        if (attachments.Any(at => at is PhotoAttachment))
                        {
                            photoUploadServer = await ApiClient.Photo.GetMessagesUploadServerAsync(peerId);
                        }
                    }), Task.Run(async () =>
                    {
                        if (attachments.Any(at => !(at is VoiceAttachment || at is PhotoAttachment)))
                        {
                            docsUploadServer =
                                await ApiClient.Docs.GetMessagesUploadServerAsync(peerId, DocMessageType.Doc);
                        }
                    }), Task.Run(async () =>
                    {
                        if (attachments.Any(at => at is VoiceAttachment))
                        {
                            voiceUploadServer =
                                await ApiClient.Docs.GetMessagesUploadServerAsync(peerId, DocMessageType.AudioMessage);
                        }
                    }));

                    #endregion

                    #region Get vk attachments

                    var groupableAttachments = attachments.Where(at => !(at is IVkSpecialAttachment));

                    Func<Attachment, Task<MediaAttachment>> GroupableAttachmentSelector()
                    {
                        return async at =>
                        {
                            switch (at)
                            {
                                case AnimationAttachment animation:
                                    return await _uploadDocument(animation, docsUploadServer);
                                case StickerAttachment sticker:
                                    return await _uploadPhoto(sticker, photoUploadServer);
                                case PhotoAttachment albumPhoto:
                                    return await _uploadPhoto(albumPhoto, photoUploadServer);
                                case VideoAttachment albumVideo:
                                    return await _uploadDocument(albumVideo, docsUploadServer);
                                case VoiceAttachment voice:
                                    return await _uploadDocument(voice, voiceUploadServer);
                                case AudioAttachment audio:
                                    return await _uploadDocument(audio, docsUploadServer, $"{audio.ToString()}.mp3.txt",
                                        "audio/mp3");
                                case FileAttachment file:
                                    return await _uploadDocument(file, docsUploadServer);
                                default:
                                    return null;
                            }
                        };
                    }

                    #endregion

                    #region Send vk attachments by chunks

                    var chunks = groupableAttachments
                        .Select((val, i) => (val, i))
                        .GroupBy(tuple => tuple.i / 10);

                    foreach (var chunk in chunks)
                    {
                        var vkAttachments =
                            await Task.WhenAll(chunk.Select(x => x.val).Select(GroupableAttachmentSelector()));

                        await ApiClient.Messages.SendAsync(new MessagesSendParams
                        {
                            GroupId = _groupId,
                            PeerId = peerId,
                            Attachments = vkAttachments
                        });
                    }

                    #endregion

                    #region Send special attachments (contacts/urls/places)

                    foreach (var at in attachments)
                    {
                        switch (at)
                        {
                            case ContactAttachment contact:
                                await ApiClient.Messages.SendAsync(new MessagesSendParams
                                {
                                    GroupId = _groupId,
                                    PeerId = peerId,
                                    Attachments = new[] {await _uploadDocument(contact, docsUploadServer)},
                                    Message = contact.ToString()
                                });
                                break;
                            case LinkAttachment link:
                                await ApiClient.Messages.SendAsync(new MessagesSendParams
                                {
                                    GroupId = _groupId,
                                    PeerId = peerId,
                                    Message = link.ToString()
                                });
                                break;
                            case PlaceAttachment place:
                                await ApiClient.Messages.SendAsync(new MessagesSendParams
                                {
                                    GroupId = _groupId,
                                    PeerId = peerId,
                                    Lat = place.Latitude,
                                    Longitude = place.Longitude, // typo in lib
                                    Message = place.ToString()
                                });
                                break;
                        }
                    }

                    #endregion
                }
                catch (Exception e)
                {
                    Logger.LogError("Attachments upload failed {0}", e);
                }
            }

            #region Send message

            var body = FormatMessageBody(message, fwd);
            if (body.Length > 0)
            {
                await ApiClient.Messages.SendAsync(new MessagesSendParams
                {
                    GroupId = _groupId,
                    PeerId = peerId,
                    Message = body
                });
            }

            #endregion
        }
    }
}