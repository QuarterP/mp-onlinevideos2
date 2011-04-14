﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Pondman.IMDb
{
    using OnlineVideos.Sites.Pondman.Interfaces;

    public class Settings : ISessionSettings
    {

        /// <summary>
        /// Gets or sets the base URI for the IMDb API.
        /// </summary>
        /// <value>a string representing the base url.</value>
        public string BaseApiUri
        {
            get
            {
                return this.baseApiUri;
            }
            set
            {
                this.baseApiUri = value;
            }
        } private string baseApiUri = "http://app.imdb.com/{0}?locale={1}{2}";

        /// <summary>
        /// GGets or sets the base URI for the IMDb website.
        /// </summary>
        /// <value>a string representing the base url.</value>
        public string BaseUri
        {
            get
            {
                return this.baseUri;
            }
            set
            {
                this.baseUri = value;
            }
        } private string baseUri = "http://www.imdb.com";

        /// <summary>
        /// Gets or sets the locale used for requests to IMDb.
        /// </summary>
        /// <value>a string representing the IMDb locale parameter</value>
        public string Locale
        {
            get 
            { 
                return this.locale; 
            }
            set 
            { 
                this.locale = value; 
            }
        } private string locale = "en_US";

        public string TitleDetails
        {
            get
            {
                return this.titleDetails;
            }
            set
            {
                this.titleDetails = value;
            }
        } private string titleDetails = "title/maindetails";

        public string FeatureComingSoon
        {
            get
            {
                return this.featureComingSoon;
            }
            set
            {
                this.featureComingSoon = value;
            }
        } private string featureComingSoon = "feature/comingsoon";

        public string ChartTop250
        {
            get
            {
                return this.chartTop250;
            }
            set
            {
                this.chartTop250 = value;
            }
        } private string chartTop250 = "chart/top";

        public string ChartBottom100
        {
            get
            {
                return this.chartBottom100;
            }
            set
            {
                this.chartBottom100 = value;
            }
        } private string chartBottom100 = "chart/bottom";

        public string VideoGallery 
        {
            get    
            { 
                return this.videoGallery; 
            }
            set 
            { 
                this.videoGallery = value; 
            }
        } private string videoGallery = "http://www.imdb.com/title/{0}/videogallery?sort=1";

        public string VideoInfo
        {
            get
            {
                return this.videoInfo;
            }
            set
            {
                this.videoInfo = value;
            }
        } private string videoInfo = "http://www.imdb.com/video/screenplay/{0}/";

        public string TrailersTopHD
        {
            get
            {
                return this.trailersTopHD;
            }
            set
            {
                this.trailersTopHD = value;
            }
        } private string trailersTopHD = "http://www.imdb.com/video/trailers/data/_json?list=top_hd";

        public string TrailersRecent
        {
            get
            {
                return this.trailersRecent;
            }
            set
            {
                this.trailersRecent = value;
            }
        } private string trailersRecent = "http://www.imdb.com/video/trailers/data/_json?list=recent";

        public string TrailersPopular
        {
            get
            {
                return this.trailersPopular;
            }
            set
            {
                this.trailersPopular = value;
            }
        } private string trailersPopular = "http://www.imdb.com/video/trailers/data/_json?list=popular";

    }
    
}