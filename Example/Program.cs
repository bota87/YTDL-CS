using System;
using System.Threading.Tasks;
using ytdl_cs;

namespace Example
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var yt = new Ytdl();
            string videoId = "V7WSrlSIF8k";
            VideoInfo info = await yt.GetVideoInfoAsync(videoId, TimeSpan.FromSeconds(5));
            const short audiomp4tag = 140;
            string url = info.Formats.Find(x => x.Itag == audiomp4tag).Url;
            Console.WriteLine(url);
        }
    }
}
