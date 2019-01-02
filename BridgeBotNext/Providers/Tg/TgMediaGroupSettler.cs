using System;
using System.Collections.Generic;
using System.Timers;
using BridgeBotNext.Attachments;

namespace BridgeBotNext.Providers.Tg
{
    public class TgMediaGroupSettler
    {
        private readonly IDictionary<string, MediaGroup> _mediaGroups = new Dictionary<string, MediaGroup>();

        public void AddMediaGroupMessage(string mediaGroupId, Message message, Attachment attachment)
        {
            lock (_mediaGroups)
            {
                // Get media group or create it
                var mediaGroup = _mediaGroups[mediaGroupId] ?? new MediaGroup(message, (sender, args) =>
                {
                    // If timer is timed out, then media group is probably fully received
                    lock (_mediaGroups)
                    {
                        if (MediaGroupReceived != null)
                            MediaGroupReceived(this,
                                new MediaGroupEventArgs(_mediaGroups[mediaGroupId].Message,
                                    _mediaGroups[mediaGroupId].Attachments.ToArray()));

                        _mediaGroups[mediaGroupId].Dispose();
                        _mediaGroups.Remove(mediaGroupId);
                    }
                });

                // Add new attachment to it
                mediaGroup.Attachments.Add(attachment);
                _mediaGroups[mediaGroupId] = mediaGroup;

                // If there is already 10 attachments, then media group is fully received
                if (mediaGroup.Attachments.Count >= 10)
                {
                    if (MediaGroupReceived != null)
                        MediaGroupReceived(this,
                            new MediaGroupEventArgs(_mediaGroups[mediaGroupId].Message,
                                _mediaGroups[mediaGroupId].Attachments.ToArray()));

                    mediaGroup.Dispose();
                    _mediaGroups.Remove(mediaGroupId);
                }
            }

            ;
        }

        /**
         * All (may be) messages with the same mediaGroupId are catched
         */
        public event EventHandler<MediaGroupEventArgs> MediaGroupReceived;

        private class MediaGroup : IDisposable
        {
            public MediaGroup(Message message, ElapsedEventHandler eventHandler)
            {
                Message = message;
                Timer.Elapsed += eventHandler;
                Timer.Start();
            }

            public List<Attachment> Attachments { get; } = new List<Attachment>();
            public Message Message { get; }
            public Timer Timer { get; } = new Timer(1500);

            public void Dispose()
            {
                Timer?.Dispose();
            }
        }

        public class MediaGroupEventArgs : EventArgs
        {
            public MediaGroupEventArgs(Message message, Attachment[] attachments)
            {
                Message = message;
                Attachments = attachments;
            }

            public Attachment[] Attachments { get; }
            public Message Message { get; }
        }
    }
}