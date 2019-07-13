using System.Collections.Generic;

namespace ytdl_cs
{
    public class VideoInfo
    {
        public string Title { get; private set; } // title
        public string VideoId { get; private set; } // video_id
        public string Author { get; private set; } // author
        public int LengthSeconds { get; private set; } // length_seconds
        public List<Format> Formats { get; private set; }

        internal VideoInfo(string title, string videoid, string author, int length, List<Format> formats)
        {
            Title = title;
            VideoId = videoid;
            Author = author;
            LengthSeconds = length;
            Formats = formats;
        }
    }
}
