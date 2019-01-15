namespace YoutubeLiveCommentReceiver
{
    public class ReceivedEventArgs
    {
        public string UserName { get; set; }
        public string Comment { get; set; }

        public ReceivedEventArgs(string userName, string comment)
        {
            UserName = userName;
            Comment = comment;
        }
    }
}