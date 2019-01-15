using Codeplex.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace YoutubeLiveCommentReceiver
{
    public class CommentReceiver
    {
        private string ApiKey;
        private System.Timers.Timer timer = null;

        private dynamic MessageObject;
        private Dictionary<string, object[]> MessagesDictionary = new Dictionary<string, object[]>();
        private List<string> MessagesIds = new List<string>();
        private List<string> MessagesIdsOld = new List<string>();
        private List<string> MessagesIdsDiff = new List<string>();

        public string LiveChatId;
        public bool ShowOwnersMessage = false;
        public event EventHandler<ReceivedEventArgs> Received;

        public CommentReceiver(string apiKey)
        {
            ApiKey = apiKey;
            if (timer != null)
            {
                timer.Dispose();
            }
            timer = new System.Timers.Timer(1000);
            timer.Elapsed += Timer_Elapsed;
        }

        public CommentReceiver(string apiKey, double interval)
        {
            ApiKey = apiKey;
            if (timer != null)
            {
                timer.Dispose();
            }
            timer = new System.Timers.Timer(interval);
            timer.Elapsed += Timer_Elapsed;
        }

        public void Start(string id)
        {
            LiveChatId = GetLiveChatId(id);
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }

        protected virtual void OnReceived(ReceivedEventArgs e)
        {
            Received?.Invoke(this, e);
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var urlBase = "https://www.googleapis.com/youtube/v3/liveChat/messages";
            var url = $"{urlBase}?part=snippet,authorDetails&liveChatId={LiveChatId}&key={ApiKey}";
            var messagesRequest = WebRequest.Create(url);

            MessageObject = null;

            try
            {
                using (var messagesResponse = messagesRequest.GetResponse())
                {
                    using (var messagesStream = new StreamReader(messagesResponse.GetResponseStream()))
                    {
                        MessageObject = DynamicJson.Parse(messagesStream.ReadToEnd());
                    }
                }
            }
            catch
            {
                Console.Error.WriteLine("Failed to get comment");
            }

            MessagesIds.Clear();
            MessagesDictionary.Clear();

            foreach (var value in MessageObject.items)
            {
                MessagesIds.Add(value.id);

                MessagesDictionary.Add(value.id, new object[]
                {
                        value.authorDetails.displayName,
                        value.snippet.textMessageDetails.messageText,
                        value.authorDetails.isChatOwner
                });
            }

            MessagesIdsDiff = new List<string>(MessagesIds);
            MessagesIdsDiff.RemoveAll(MessagesIdsOld.Contains);

            foreach (var value in MessagesIdsDiff)
            {
                if (ShowOwnersMessage || !Convert.ToBoolean(MessagesDictionary[value][2]))
                {
                    var messageSender = MessagesDictionary[value][0] as string;
                    var messageText = MessagesDictionary[value][1] as string;
                    var arg = new ReceivedEventArgs(messageSender, messageText);
                    OnReceived(arg);
                }
            }

            MessagesIdsOld.Clear();
            MessagesIdsOld = new List<string>(MessagesIds);

            MessagesIdsDiff.Clear();
        }

        public string GetVideoId(string id)
        {
            if (id.Length == 11)
            {
                return id;
            }
            if (id.Length != 24)
            {
                throw new Exception();
            }

            var urlBase = "https://www.youtube.com/channel/";
            var url = urlBase + id + "/videos?flow=list&live_view=501&view=2";
            var videoIdRequest = WebRequest.Create(url);

            try
            {
                using (var videoIdResponse = videoIdRequest.GetResponse())
                {
                    using (var videoIdStream = new StreamReader(videoIdResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        var regexPattern = "href=\"\\/watch\\?v=(.+?)\"";
                        var videoIdRegex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

                        var videoIdMatch = videoIdRegex.Match(videoIdStream.ReadToEnd());

                        if (!videoIdMatch.Success)
                        {
                            throw new Exception("Streaming not found");
                        }

                        var index1 = videoIdMatch.Value.LastIndexOf('=') + 1;
                        var index2 = videoIdMatch.Value.LastIndexOf('"');

                        return videoIdMatch.Value.Substring(index1, index2 - index1);
                    }
                }
            }
            catch
            {
                throw new Exception("Faild to search streaming");
            }
        }

        public string GetLiveChatId(string id)
        {
            string liveChatId = "";
            var videoId = GetVideoId(id);
            var urlBase = "https://www.googleapis.com/youtube/v3/videos";
            var url = $"{urlBase}?part=liveStreamingDetails&id={videoId}&key={ApiKey}";
            var liveChatIdRequest = WebRequest.Create(url);

            try
            {
                using (var liveChatIdResponse = liveChatIdRequest.GetResponse())
                {
                    using (var liveChatIdStream = new StreamReader(liveChatIdResponse.GetResponseStream(), Encoding.UTF8))
                    {
                        var liveChatIdObject = DynamicJson.Parse(liveChatIdStream.ReadToEnd());

                        liveChatId = liveChatIdObject.items[0].liveStreamingDetails.activeLiveChatId;

                        if (liveChatId == null)
                        {
                            throw new Exception("Failed to get Live Chat ID");
                        }
                    }
                }
            }
            catch
            {
                throw new Exception("Failed to get Live Chat ID");
            }
            return liveChatId;
        }
    }
}
