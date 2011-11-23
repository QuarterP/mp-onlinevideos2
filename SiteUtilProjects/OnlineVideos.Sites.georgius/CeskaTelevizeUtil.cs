﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;

namespace OnlineVideos.Sites.georgius
{
    public sealed class CeskaTelevizeUtil : SiteUtilBase
    {
        #region Private fields

        private static String baseUrl = "http://www.ceskatelevize.cz";
        private static String dynamicCategoryBaseUrl = "http://www.ceskatelevize.cz/ivysilani/podle-abecedy";

        private static String dynamicCategoryStart = @"<ul id=""programmeGenre"" class=""clearfix"">";
        private static String dynamicCategoryRegex = @"<a class=""pageLoadAjaxAlphabet"" href=""(?<dynamicCategoryUrl>[^""]+)"" rel=""[^""]*""><span>(?<dynamicCategoryTitle>[^<]+)";

        private static String subCategoryFormat = @"{0}{1}dalsi-casti";

        private static String showListStart = @"<div id=""programmeAlphabetContent"">";
        private static String showRegex = @"(<a href=""(?<showUrl>[^""]+)"" title=""[^""]*"">(?<showTitle>[^""]+)</a>)|(<a class=""toolTip"" href=""(?<showUrl>[^""]+)"" title=""[^""]*"">(?<showTitle>[^""]+)</a>)";
        private static String showEnd = @"</li>";
        private static string onlyBonuses = @"<span class=""labelBonus"">pouze bonusy</span>";

        private static String showEpisodesStartRegex = @"(<div class=""contentBox"">)|(<div class=""clearfix"">)";
        private static String showEpisodeBlockStartRegex = @"(<li class=""itemBlock clearfix"">)|(<li class=""itemBlock clearfix active"">)|(<div class=""channel"">)";
        private static String showEpisodeThumbUrlRegex = @"src=""(?<showThumbUrl>[^""]+)""";
        private static String showEpisodeUrlAndTitleRegex = @"(<a class=""itemSetPaging"" rel=""[^""]+"" href=""(?<showUrl>[^""]+)"">(?<showTitle>[^<]+)</a>)|(<a href=""(?<showUrl>[^""]+)"">(?<showTitle>[^<]+)</a>)";
        private static String showEpisodeNextPageRegex = @"<a title=""[^""]+"" rel=""[^""]*"" class=""detailProgrammePaging next"" href=""(?<url>[^""]+)"">";

        private static String liveNextProgramm = @"<p class=""next"">";

        private static String showEpisodePostStart = @"callSOAP(";
        private static String showEpisodePostEnd = @");";

        // the number of show episodes per page
        private static int pageSize = 28;

        private int currentStartIndex = 0;
        private Boolean hasNextPage = false;

        private List<VideoInfo> loadedEpisodes = new List<VideoInfo>();
        private String nextPageUrl = String.Empty;

        private RssLink currentCategory = new RssLink();

        #endregion

        #region Constructors

        public CeskaTelevizeUtil()
            : base()
        {
        }

        #endregion

        #region Methods

        public override void Initialize(SiteSettings siteSettings)
        {
            base.Initialize(siteSettings);
        }

        public override int DiscoverDynamicCategories()
        {
            int dynamicCategoriesCount = 0;

            this.Settings.Categories.Add(
                new RssLink()
                {
                    Name = "Živě",
                    HasSubCategories = false,
                    Url = "live"
                });
            dynamicCategoriesCount++;

            String baseWebData = SiteUtilBase.GetWebData(CeskaTelevizeUtil.dynamicCategoryBaseUrl, null, null, null, true);

            int index = baseWebData.IndexOf(CeskaTelevizeUtil.dynamicCategoryStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);
                Match match = Regex.Match(baseWebData, CeskaTelevizeUtil.dynamicCategoryRegex);
                while (match.Success)
                {
                    String dynamicCategoryUrl = match.Groups["dynamicCategoryUrl"].Value;
                    String dynamicCategoryTitle = match.Groups["dynamicCategoryTitle"].Value;

                    this.Settings.Categories.Add(
                        new RssLink()
                        {
                            Name = dynamicCategoryTitle,
                            HasSubCategories = true,
                            Url = String.Format("{0}{1}", CeskaTelevizeUtil.baseUrl, dynamicCategoryUrl)
                        });

                    dynamicCategoriesCount++;
                    match = match.NextMatch();
                }
            }

            this.Settings.DynamicCategoriesDiscovered = true;
            return dynamicCategoriesCount;
        }

        private List<VideoInfo> GetPageVideos(String pageUrl)
        {
            List<VideoInfo> pageVideos = new List<VideoInfo>();

            if (!String.IsNullOrEmpty(pageUrl))
            {
                if (this.currentCategory.Name == "Živě")
                {
                    TimeSpan span = DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0);
                    pageUrl = "http://www.ceskatelevize.cz/ivysilani/ajax/liveBox.php?time=" + ((long)span.TotalMilliseconds).ToString();
                }
                String baseWebData = SiteUtilBase.GetWebData(pageUrl, null, null, null, true);

                Match showEpisodesStart = Regex.Match(baseWebData, CeskaTelevizeUtil.showEpisodesStartRegex);
                if (showEpisodesStart.Success)
                {
                    baseWebData = baseWebData.Substring(showEpisodesStart.Index + showEpisodesStart.Length);

                    Match nextPageMatch = Regex.Match(baseWebData, CeskaTelevizeUtil.showEpisodeNextPageRegex);
                    while (true)
                    {
                        Match showEpisodeBlockStart = Regex.Match(baseWebData, CeskaTelevizeUtil.showEpisodeBlockStartRegex);
                        nextPageMatch = Regex.Match(baseWebData, CeskaTelevizeUtil.showEpisodeNextPageRegex);

                        if (((nextPageMatch.Success) && (showEpisodeBlockStart.Success) && (nextPageMatch.Index > showEpisodeBlockStart.Index)) ||
                            ((!nextPageMatch.Success) && (showEpisodeBlockStart.Success)))
                        {
                            baseWebData = baseWebData.Substring(showEpisodeBlockStart.Index + showEpisodeBlockStart.Length);

                            String showTitle = String.Empty;
                            String showThumbUrl = String.Empty;
                            String showUrl = String.Empty;

                            if (this.currentCategory.Name == "Živě")
                            {
                                int nextProgrammIndex = baseWebData.IndexOf(CeskaTelevizeUtil.liveNextProgramm);
                                if (nextProgrammIndex >= 0)
                                {
                                    String liveChannelData = baseWebData.Substring(0, nextProgrammIndex);

                                    Match showEpisodeUrlAndTitle = Regex.Match(liveChannelData, CeskaTelevizeUtil.showEpisodeUrlAndTitleRegex);
                                    if (showEpisodeUrlAndTitle.Success)
                                    {
                                        showUrl = Utils.FormatAbsoluteUrl(showEpisodeUrlAndTitle.Groups["showUrl"].Value, CeskaTelevizeUtil.baseUrl);
                                        showTitle = showEpisodeUrlAndTitle.Groups["showTitle"].Value.Trim();
                                    }

                                    Match showEpisodeThumbUrl = Regex.Match(liveChannelData, CeskaTelevizeUtil.showEpisodeThumbUrlRegex);
                                    if (showEpisodeThumbUrl.Success)
                                    {
                                        showThumbUrl = showEpisodeThumbUrl.Groups["showThumbUrl"].Value;
                                    }

                                    baseWebData = baseWebData.Substring(nextProgrammIndex);
                                }
                            }
                            else
                            {
                                Match showEpisodeThumbUrl = Regex.Match(baseWebData, CeskaTelevizeUtil.showEpisodeThumbUrlRegex);
                                if (showEpisodeThumbUrl.Success)
                                {
                                    showThumbUrl = showEpisodeThumbUrl.Groups["showThumbUrl"].Value;
                                    baseWebData = baseWebData.Substring(showEpisodeThumbUrl.Index + showEpisodeThumbUrl.Length);
                                }

                                Match showEpisodeUrlAndTitle = Regex.Match(baseWebData, CeskaTelevizeUtil.showEpisodeUrlAndTitleRegex);
                                if (showEpisodeUrlAndTitle.Success)
                                {
                                    showUrl = Utils.FormatAbsoluteUrl(showEpisodeUrlAndTitle.Groups["showUrl"].Value, CeskaTelevizeUtil.baseUrl);
                                    showTitle = showEpisodeUrlAndTitle.Groups["showTitle"].Value.Trim();
                                    baseWebData = baseWebData.Substring(showEpisodeUrlAndTitle.Index + showEpisodeUrlAndTitle.Length);
                                }
                            }

                            if (String.IsNullOrEmpty(showTitle) || String.IsNullOrEmpty(showUrl) || String.IsNullOrEmpty(showThumbUrl))
                            {
                                continue;
                            }

                            VideoInfo videoInfo = new VideoInfo()
                            {
                                ImageUrl = showThumbUrl,
                                Title = showTitle,
                                VideoUrl = showUrl
                            };

                            pageVideos.Add(videoInfo);
                        }
                        else
                        {
                            break;
                        }
                    }

                    this.nextPageUrl = (nextPageMatch.Success) ? String.Format("{0}{1}", CeskaTelevizeUtil.baseUrl, nextPageMatch.Groups["url"].Value) : String.Empty;
                }
            }

            return pageVideos;
        }

        private List<VideoInfo> GetVideoList(Category category, int videoCount)
        {
            hasNextPage = false;
            String baseWebData = String.Empty;
            RssLink parentCategory = (RssLink)category;
            List<VideoInfo> videoList = new List<VideoInfo>();

            if (parentCategory.Name != this.currentCategory.Name)
            {
                this.currentStartIndex = 0;
                this.nextPageUrl = parentCategory.Url;
                this.loadedEpisodes.Clear();
            }

            this.currentCategory = parentCategory;
            int addedVideos = 0;

            while (true)
            {
                while (((this.currentStartIndex + addedVideos) < this.loadedEpisodes.Count()) && (addedVideos < videoCount))
                {
                    videoList.Add(this.loadedEpisodes[this.currentStartIndex + addedVideos]);
                    addedVideos++;
                }

                if (addedVideos < videoCount)
                {
                    List<VideoInfo> loadedVideos = this.GetPageVideos(this.nextPageUrl);

                    if (loadedVideos.Count == 0)
                    {
                        break;
                    }
                    else
                    {
                        this.loadedEpisodes.AddRange(loadedVideos);
                    }
                }
                else
                {
                    break;
                }
            }

            if (((this.currentStartIndex + addedVideos) < this.loadedEpisodes.Count()) || (!String.IsNullOrEmpty(this.nextPageUrl)))
            {
                hasNextPage = true;
            }

            this.currentStartIndex += addedVideos;

            return videoList;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            this.currentStartIndex = 0;
            return this.GetVideoList(category, CeskaTelevizeUtil.pageSize - 2);
        }

        public override List<VideoInfo> getNextPageVideos()
        {
            return this.GetVideoList(this.currentCategory, CeskaTelevizeUtil.pageSize);
        }

        public override bool HasNextPage
        {
            get
            {
                return this.hasNextPage;
            }
            protected set
            {
                this.hasNextPage = value;
            }
        }

        private String SerializeJsonForPost(Newtonsoft.Json.Linq.JToken token)
        {
            Newtonsoft.Json.JsonSerializer serializer = Newtonsoft.Json.JsonSerializer.Create(null);
            StringBuilder builder = new StringBuilder();
            serializer.Serialize(new CeskaTelevizeJsonTextWriter(new System.IO.StringWriter(builder), token), token);
            return builder.ToString();
        }

        public override List<string> getMultipleVideoUrls(VideoInfo video, bool inPlaylist = false)
        {
            List<String> resultUrls = new List<string>();

            String baseWebData = SiteUtilBase.GetWebData(video.VideoUrl, null, null, null, true);

            int start = baseWebData.IndexOf(CeskaTelevizeUtil.showEpisodePostStart);
            if (start >= 0)
            {
                start += CeskaTelevizeUtil.showEpisodePostStart.Length;
                int end = baseWebData.IndexOf(CeskaTelevizeUtil.showEpisodePostEnd, start);
                if (end >= 0)
                {
                    String postData = baseWebData.Substring(start, end - start);

                    Newtonsoft.Json.Linq.JObject jObject = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.JsonConvert.DeserializeObject(postData);
                    String serializedDataForPost = this.SerializeJsonForPost(jObject);
                    serializedDataForPost = HttpUtility.UrlEncode(serializedDataForPost).Replace("%3d", "=").Replace("%26", "&");
                    String videoDataUrl = SiteUtilBase.GetWebDataFromPost("http://www.ceskatelevize.cz/ajax/playlistURL.php", serializedDataForPost);

                    XmlDocument videoData = new XmlDocument();
                    videoData.LoadXml(SiteUtilBase.GetWebData(videoDataUrl));

                    XmlNodeList videoItems = videoData.SelectNodes("//PlaylistItem[@id]");
                    foreach (XmlNode videoItem in videoItems)
                    {
                        if (videoItem.Attributes["id"].Value.IndexOf("ad", StringComparison.CurrentCultureIgnoreCase) == (-1))
                        {
                            // skip advertising
                            XmlNode itemData = videoData.SelectSingleNode(String.Format("//switchItem[@id = \"{0}\"]", videoItem.Attributes["id"].Value));
                            if (itemData != null)
                            {
                                // now select source with highest bitrate
                                XmlNodeList sources = itemData.SelectNodes("./video");
                                XmlNode source = null;
                                int bitrate = 0;
                                foreach (XmlNode tempSource in sources)
                                {
                                    int tempBitrate = int.Parse(tempSource.Attributes["system-bitrate"].Value);
                                    if (tempBitrate > bitrate)
                                    {
                                        bitrate = tempBitrate;
                                        source = tempSource;
                                    }
                                }

                                if (source != null)
                                {
                                    // create rtmp proxy for selected source
                                    String baseUrl = itemData.Attributes["base"].Value.Replace("/_definst_", "");
                                    String playPath = source.Attributes["src"].Value;

                                    String rtmpUrl = baseUrl + "/" + playPath;

                                    String host = new Uri(baseUrl).Host;
                                    String app = baseUrl.Substring(baseUrl.LastIndexOf('/') + 1);
                                    String tcUrl = baseUrl;

                                    int swfobjectIndex = baseWebData.IndexOf("swfobject.embedSWF(");
                                    if (swfobjectIndex >= 0)
                                    {
                                        int firstQuote = baseWebData.IndexOf("\"", swfobjectIndex);
                                        int secondQuote = baseWebData.IndexOf("\"", firstQuote + 1);

                                        if ((firstQuote >= 0) && (secondQuote >= 0) && ((secondQuote - firstQuote) > 0))
                                        {
                                            String swfUrl = baseWebData.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                                            String resultUrl = new OnlineVideos.MPUrlSourceFilter.RtmpUrl(rtmpUrl) { TcUrl = tcUrl, App = app, PlayPath = playPath, SwfUrl = swfUrl, PageUrl = video.VideoUrl }.ToString();
                                            
                                            resultUrls.Add(resultUrl);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return resultUrls;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            int dynamicSubCategoriesCount = 0;
            RssLink category = (RssLink)parentCategory;

            String baseWebData = SiteUtilBase.GetWebData(category.Url, null, null, null, true);

            int index = baseWebData.IndexOf(CeskaTelevizeUtil.showListStart);
            if (index > 0)
            {
                baseWebData = baseWebData.Substring(index);
                category.SubCategories = new List<Category>();

                MatchCollection matches = Regex.Matches(baseWebData, CeskaTelevizeUtil.showRegex);
                for (int i = 0; i < matches.Count; i++)
                {
                    String showUrl = matches[i].Groups["showUrl"].Value;
                    String showTitle = matches[i].Groups["showTitle"].Value;

                    int showEndIndex = baseWebData.IndexOf(CeskaTelevizeUtil.showEnd, matches[i].Index + matches[i].Length);
                    int onlyBonusesIndex = baseWebData.IndexOf(CeskaTelevizeUtil.onlyBonuses, matches[i].Index + matches[i].Length);

                    if (((onlyBonusesIndex != (-1)) && (onlyBonusesIndex > showEndIndex)) ||
                        (onlyBonusesIndex == (-1)))
                    {
                        category.SubCategoriesDiscovered = true;
                        category.SubCategories.Add(
                            new RssLink()
                            {
                                Name = showTitle,
                                HasSubCategories = false,
                                Url = String.Format(CeskaTelevizeUtil.subCategoryFormat, CeskaTelevizeUtil.baseUrl, showUrl)
                            });

                        dynamicSubCategoriesCount++;
                    }
                }
            }

            return dynamicSubCategoriesCount;
        }

        #endregion
    }
}