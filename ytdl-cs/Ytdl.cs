using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace ytdl_cs
{
    public class Ytdl
    {

        private const string VIDEO_EURL = "https://youtube.googleapis.com/v/";
        private const string INFO_HOST = "www.youtube.com";
        private const string INFO_PATH = "/get_video_info";

        private SignatureCipherManager signatureCipherManager = new SignatureCipherManager();

        public async Task<VideoInfo> GetVideoInfoAsync(string videoId, TimeSpan timeout)
        {
            // Maybe consider extracting video_id from this parameter so links can be passed?

            NameValueCollection info = await GetVideoInfoRawAsync(videoId, timeout);

            if (info == null)
            {
                return null;
                // TODO: Throw errors on specific operations.
            }

            JObject pr = JObject.Parse(info["player_response"]);
            var vd = pr["videoDetails"];

            string title = (string)vd["title"];
            string author = (string)vd["author"];
            int length = int.Parse((string)vd["lengthSeconds"]);

            Format[] fmts = ParseFormats(info);
            string[] tokens = await signatureCipherManager.GetTokensAsync(info, timeout);

            List<Format> decipheredFormats = DecipherFormats(fmts, tokens);

            return new VideoInfo(title, videoId, author, length, decipheredFormats);
        }

        public async Task<NameValueCollection> GetVideoInfoRawAsync(string videoId, TimeSpan timeout)
        {
            JObject config = await GetPageConfigAsync(videoId, timeout);

            if (config == null)
            {
                return null;
            }

            var sts = (string)config.GetValue("sts");

            string videoInfo;
            using (var httpClient = new HttpClient())
            {
                var builder = new UriBuilder("https://" + INFO_HOST + INFO_PATH);
                builder.Port = -1;
                var query = HttpUtility.ParseQueryString(builder.Query);
                query["video_id"] = videoId;
                query["eurl"] = VIDEO_EURL + videoId;
                query["ps"] = "default";
                query["gl"] = "US";
                query["hl"] = "en";
                query["sts"] = sts;
                builder.Query = query.ToString();

                httpClient.Timeout = timeout;
                videoInfo = await httpClient.GetStringAsync(builder.ToString());
            }

            NameValueCollection info = HttpUtility.ParseQueryString(videoInfo);

            string html5player = (string)((JObject)config.GetValue("assets")).GetValue("js");
            info.Add("html5player", html5player);
            return info;
        }

        public async Task<JObject> GetPageConfigAsync(string videoId, TimeSpan timeout)
        {
            string result;
            using (var httpClient = new HttpClient())
            {
                var builder = new UriBuilder("https://youtube.com/watch?v=" + videoId);
                builder.Port = -1;
                var query = HttpUtility.ParseQueryString(builder.Query);
                query["hl"] = "en";
                query["bpctr"] = (DateTime.Now.Ticks / 1000).ToString();
                builder.Query = query.ToString();

                httpClient.Timeout = timeout;
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/4.0"); // senza su xamarin forms con android non mi ritornava ytplayer.config 
                result = await httpClient.GetStringAsync(builder.ToString());
            }
            string configJson = DataFormatTools.ExtractBetween(result, "ytplayer.config = ", ";ytplayer.load");

            if (configJson == null)
            {
                return null;
            }

            return JObject.Parse(configJson);
        }

        private Format[] ParseFormats(NameValueCollection videoInfo)
        {
            List<string> formats = new List<string>();

            if (videoInfo["url_encoded_fmt_stream_map"] != null)
            {
                formats.AddRange(videoInfo["url_encoded_fmt_stream_map"].Split(','));
            }

            if (videoInfo["adaptive_fmts"] != null)
            {
                formats.AddRange(videoInfo["adaptive_fmts"].Split(','));
            }

            return formats.Select(x =>
                {
                    NameValueCollection properties = HttpUtility.ParseQueryString(x);
                    string[] types = properties["type"].Split(';');
                    string type = types[0].Trim();
                    string codecs = HttpUtility.ParseQueryString(types[1].Trim())["codecs"].Replace("\"", "");
                    return new Format(properties["url"],
                        properties["itag"],
                        properties["quality"],
                        type,
                        codecs,
                        properties["s"],
                        properties["sp"]);
                }
            ).ToArray();
        }

        private List<Format> DecipherFormats(Format[] formats, string[] tokens)
        {
            List<Format> deciphered = new List<Format>();

            foreach (Format fmt in formats)
            {
                if (fmt.Url == null)
                {
                    continue;
                }

                string signature = fmt.Sig != null ? signatureCipherManager.Decipher(tokens, fmt.Sig) : null;
                Uri decodedUrl = new Uri(HttpUtility.UrlDecode(fmt.Url));
                string baseUrl = decodedUrl.GetLeftPart(UriPartial.Path);
                NameValueCollection urlParams = HttpUtility.ParseQueryString(decodedUrl.Query);
                urlParams.Remove("search");
                urlParams.Set("ratebypass", "yes");

                if (signature != null)
                {
                    if (fmt.sp != null)
                        urlParams.Set(fmt.sp, signature);
                    else
                        urlParams.Set("signature", signature);
                    fmt.Sig = signature;
                }

                string finalUrl = baseUrl + "?" + urlParams;
                fmt.Url = finalUrl;

                deciphered.Add(fmt);
            }

            return deciphered;

        }
    }
}
