using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

using BridgeBotNext.Attachments;
using BridgeBotNext.Configuration;
using BridgeBotNext.Entities;

using Caching;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MoreLinq.Extensions;

using SkiaSharp;

using VkBotFramework;

using VkNet;
using VkNet.Abstractions;
using VkNet.Enums;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

using Attachment = BridgeBotNext.Attachments.Attachment;
using Conversation = BridgeBotNext.Entities.Conversation;
using Message = BridgeBotNext.Entities.Message;

namespace BridgeBotNext.Providers.Vk
{
    public class VkProvider : Provider
    {
        private readonly string _accessToken;
        protected readonly IVkApi ApiClient;
        protected readonly VkBot VkBot;
        private readonly ulong _groupId;
        protected ILogger<VkProvider> Logger;
        protected LRUCache<long, string> DisplayNameCache;
        private readonly Random random = new Random();

        public VkProvider(ILogger<VkProvider> logger, ILogger<VkBot> botLogger, ILogger<VkApi> apiLogger,
            IOptions<VkConfiguration> configuration)
        {
            _accessToken = configuration.Value.AccessToken;
            _groupId = configuration.Value.GroupId;
            Logger = logger;
            VkBot = new VkBot(_accessToken, (int)_groupId, botLogger);
            ApiClient = new VkApi(apiLogger);
            ApiClient.Authorize(new ApiAuthParams
            {
                AccessToken = _accessToken
            });
            DisplayNameCache = new LRUCache<long, string>(1000, 6);

            VkBot.OnMessageReceived += _onMessage;
        }

        public override string Name => "vk";
        public override string DisplayName => "Vkontakte";

        private async Task<Message> _extractMessage(VkNet.Model.Message msg, bool extractConversation = true)
        {
            Conversation conversation = null;
            Person person = null;
            IEnumerable<Message> forwarded = null;
            IEnumerable<Attachment> attachments = null;
            await Task.WhenAll(
                Task.Run(async () =>
                {
                    if (extractConversation) conversation = await _extractConversation(msg);
                }),
                Task.Run(async () =>
                {
                    person = await _extractPerson(msg);
                }),
                Task.Run(async () =>
                {
                    forwarded = await Task.WhenAll(msg.ForwardedMessages.Select(fwd => _extractMessage(fwd, false)));
                }),
                Task.Run(() =>
                {
                    attachments = _extractAttachments(msg);
                })
            );
            var body = msg.Text ?? "";
            return new Message(conversation, person, body, forwarded, attachments);
        }

        private List<Attachment> _extractAttachments(VkNet.Model.Message msg)
        {
            if (msg.Attachments.IsNullOrEmpty() && msg.Geo == null) return null;

            var attachments = new List<Attachment>();

            foreach (var at in msg.Attachments)
                if (at.Instance is Photo photo)
                {
                    var maxPhoto = photo.Sizes.MaxBy(p => p.Width).First();
                    attachments.Add(new PhotoAttachment(maxPhoto.Url.ToString(), photo, photo.Text,
                        mimeType: "image/jpeg"));
                }
                else if (at.Instance is Video video)
                {
                    attachments.Add(new LinkAttachment($"https://vk.com/video{video.OwnerId}_{video.Id}", video, $"📹{video.Title}"));
                }
                else if (at.Instance is Audio audio)
                {
                    var audioName = $"{audio.Title.Replace('+', ' ')} - {audio.Artist.Replace('+', ' ')}";
                    attachments.Add(new LinkAttachment(
                        $"https://vk.com/audio?q={HttpUtility.UrlEncode(audioName)}", audio, $"🎵{audioName}"));
                }
                else if (at.Instance is Document doc)
                {
                    if (doc.Type == DocumentTypeEnum.Gif)
                        attachments.Add(new AnimationAttachment(doc.Uri, doc, doc.Title, fileSize: doc.Size ?? 0));
                    else if (doc.Type == DocumentTypeEnum.Video)
                        attachments.Add(new VideoAttachment(doc.Uri, doc, doc.Title, fileSize: doc.Size ?? 0));
                    else
                        attachments.Add(new FileAttachment(doc.Uri, doc, doc.Title, fileSize: doc.Size));
                }
                else if (at.Instance is Link link)
                {
                    attachments.Add(new LinkAttachment(link.Uri.ToString(), link, $"🔗{link.Title}"));
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
                    var maxSticker = sticker.Images.MaxBy(s => s.Width).First();
                    attachments.Add(new StickerAttachment(maxSticker.Url.ToString(), sticker));
                }
                else if (at.Instance is Gift gift)
                {
                    var giftUri = gift.Thumb256 ?? gift.Thumb96 ?? gift.Thumb48;
                    attachments.Add(new PhotoAttachment(giftUri.ToString(), gift, "<gift>"));
                }

            if (msg.Geo != null)
            {
                attachments.Add(new PlaceAttachment(msg.Geo.Coordinates.Latitude, msg.Geo.Coordinates.Longitude, msg.Geo.Place.Title, msg.Geo.Place.Address));
            }

            return attachments;
        }

        private async Task<Person> _extractPerson(VkNet.Model.Message msg)
        {
            try
            {
                if (msg.FromId == null || msg.FromId < 1)
                    throw new Exception("Invalid user id");

                var userId = (long)msg.FromId;
                string displayName = null;
                if (!DisplayNameCache.TryGetValue(userId, out displayName))
                {
                    var users = await ApiClient.Users.GetAsync(new[] { userId });
                    if (users.Any())
                    {
                        displayName = $"{users.First().FirstName} {users.First().LastName}".Trim();
                        DisplayNameCache.Add(userId, displayName);
                    }
                }

                return new VkPerson(this, msg.FromId.ToString(), displayName);
            }
            catch (Exception)
            {
                var id = msg.FromId?.ToString() ?? "0";
                return new VkPerson(this, id, $"[id{id}]");
            }
        }

        private async Task<Conversation> _extractConversation(VkNet.Model.Message msg)
        {
            if (msg.PeerId == null)
            {
                throw new NullReferenceException("Peer id can not be null");
            }

            var peerId = (long)msg.PeerId;
            var vkConversation = await ApiClient.Messages.GetConversationsByIdAsync(new[] { peerId }, null, null, _groupId);
            var title = vkConversation.Items.Any()
                ? vkConversation.Items.First()?.ChatSettings?.Title ?? $"#{peerId}"
                : $"#{peerId}";
            return new Conversation(this, peerId.ToString(), title);
        }

        private async void _onMessage(object sender, VkBot.MessageReceivedEventArgs e)
        {
            Logger.LogTrace("Message received");
            var message = await _extractMessage(e.message);

            if (!string.IsNullOrEmpty(message.Body) && message.Body.StartsWith("/"))
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

        public override void Dispose()
        {
            VkBot.Dispose();
        }

        private async Task<byte[]> _downloadFile(string url)
        {
            if (url.StartsWith("data:"))
            {
                var parts = url.Split(',', 2);
                if (parts.Length != 2) throw new FormatException("Data URL does not contain separator");

                return Convert.FromBase64String(parts[1]);
            }

            var wc = new WebClient();
            return await wc.DownloadDataTaskAsync(new Uri(url));
        }

        private async Task<string> _uploadFile(FileAttachment at, UploadServerInfo server,
            string overrideFileName = null, string overrideMimeType = null)
        {
            var file = await _downloadFile(at.Url);

            if (at.MimeType.StartsWith("image/") && at.MimeType != "image/png" && at.MimeType != "image/jpeg" && at.MimeType != "image/gif")
            {
                var image = SKImage.FromBitmap(SKBitmap.Decode(file));
                using (var p = image.Encode(SKEncodedImageFormat.Png, 100))
                using (MemoryStream ms = new MemoryStream())
                {
                    p.AsStream().CopyTo(ms);
                    file = ms.ToArray();
                    overrideMimeType = "image/png";
                    overrideFileName = "image.png";
                }
            }

            var postParameters = new Dictionary<string, object>
            {
                {
                    "file",
                    new FormUpload.FileParameter(file, overrideFileName ?? at.FileName, overrideMimeType ?? at.MimeType)
                }
            };

            using (var webResponse =
                FormUpload.MultipartFormDataPost(server?.UploadUrl, "MeBridgeBot/2", postParameters))
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
                return document[0].Instance;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to upload document to vk server");
            }

            return null;
        }

        private async Task<MediaAttachment> _uploadPhoto(FileAttachment ph, UploadServerInfo server)
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
            Logger.LogTrace("Send message to conversation {0}", conversation.OriginId);
            var peerId = Convert.ToInt32(conversation.OriginId);

            #region Get forwarded and attachments

            var fwd = FlattenForwardedMessages(message);
            var attachments = GetAllAttachments(message, fwd);

            #endregion

            #region Send message

            var body = FormatMessageBody(message, fwd);
            if (body.Length > 0)
                await ApiClient.Messages.SendAsync(new MessagesSendParams
                {
                    GroupId = _groupId,
                    PeerId = peerId,
                    Message = body,
                    RandomId = random.Next()
                });

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
                        if (attachments.Any(at => (at is PhotoAttachment || at is StickerAttachment)))
                            photoUploadServer = await ApiClient.Photo.GetMessagesUploadServerAsync(peerId);
                    }), Task.Run(async () =>
                    {
                        if (attachments.Any(at => !(at is VoiceAttachment || at is PhotoAttachment)))
                            docsUploadServer =
                                await ApiClient.Docs.GetMessagesUploadServerAsync(peerId, DocMessageType.Doc);
                    }), Task.Run(async () =>
                    {
                        if (attachments.Any(at => at is VoiceAttachment))
                            voiceUploadServer =
                                await ApiClient.Docs.GetMessagesUploadServerAsync(peerId, DocMessageType.AudioMessage);
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
                            Attachments = vkAttachments,
                            RandomId = random.Next()
                        });
                    }

                    #endregion

                    #region Send special attachments (contacts/urls/places)

                    foreach (var at in attachments)
                        switch (at)
                        {
                            case ContactAttachment contact:
                                await ApiClient.Messages.SendAsync(new MessagesSendParams
                                {
                                    GroupId = _groupId,
                                    PeerId = peerId,
                                    Attachments = new[] { await _uploadDocument(contact, docsUploadServer) },
                                    Message = contact.ToString(),
                                    RandomId = random.Next()
                                });
                                break;
                            case LinkAttachment link:
                                await ApiClient.Messages.SendAsync(new MessagesSendParams
                                {
                                    GroupId = _groupId,
                                    PeerId = peerId,
                                    Message = link.ToString(),
                                    RandomId = random.Next()
                                });
                                break;
                            case PlaceAttachment place:
                                await ApiClient.Messages.SendAsync(new MessagesSendParams
                                {
                                    GroupId = _groupId,
                                    PeerId = peerId,
                                    Lat = place.Latitude,
                                    Longitude = place.Longitude, // typo in lib
                                    Message = place.ToString(),
                                    RandomId = random.Next()
                                });
                                break;
                        }

                    #endregion
                }
                catch (Exception e)
                {
                    Logger.LogError("Attachments upload failed {0}", e);
                }
            }

        }
    }
}