# YoutubeLiveCommentReceiver
Library for getting YouTube Live Streaming comments

## Install
Add YoutubeLiveCommentReceiver.dll to reference

## Usage
```csharp
using System;
using YoutubeLiveCommentReceiver;

class Program
{
    static void Main(string[] args)
    {
        var apiKey = "YOUR API KEY";
        var id = Console.ReadLine(); //https://www.youtube.com/watch?v={id}
        var Receiver = new CommentReceiver(apiKey);
        Receiver.Received += Receiver_Received;
        Receiver.Start(id);
        Console.ReadLine();
    }

    private static void Receiver_Received(object sender, ReceivedEventArgs e)
    {
        Console.WriteLine(e.UserName + " : " + e.Comment);
    }
}
```

## License
MIT License

##
get_youtubelive_comments Copyright (c) 2017 midorigoke https://github.com/midorigoke/get_youtubelive_comments/blob/master/LICENSE
