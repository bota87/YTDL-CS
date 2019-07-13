using System;
using ytdl_cs;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var yt = new Ytdl();
            string videoId = "V7WSrlSIF8k";
            VideoInfo info = yt.GetVideoInfo(videoId);
            const short audiomp4tag = 140;
            string url = info.Formats.Find(x => x.Itag == audiomp4tag).Url;
            Console.WriteLine(url);
        }
    }
}
