namespace ytdl_cs
{
    public class Format
    {
        public string Url { get; internal set; }
        public short Itag { get; internal set; }
        public string Quality { get; internal set; }
        public string Type { get; internal set; }
        public string Codecs { get; internal set; }
        public string Sig { get; internal set; }
        public string sp { get; set; }

        internal Format(string url, short itag, string quality, string type, string codecs, string sig, string sp)
        {
            Url = url;
            Itag = itag;
            Quality = quality;
            Type = type;
            Codecs = codecs;
            Sig = sig;
            this.sp = sp;
        }
    }
}
