using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Owl.Notifications
{
    /// <summary>A managed copy of an FCM message; safe to retain after the SDK callback.</summary>
    public sealed class PushNotification
    {
        public string MessageId { get; }
        public string From { get; }
        public string Title { get; }
        public string Body { get; }
        public IReadOnlyDictionary<string, string> Data { get; }

        public PushNotification(string messageId, string from, string title, string body,
            IDictionary<string, string> data)
        {
            MessageId = messageId ?? string.Empty;
            From = from ?? string.Empty;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            Data = new ReadOnlyDictionary<string, string>(data == null
                ? new Dictionary<string, string>() : new Dictionary<string, string>(data));
        }
    }
}
