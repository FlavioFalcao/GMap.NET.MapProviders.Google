using System;
using System.IO;
using System.Net;
using GMap.NET.Internals;
using Google.Maps.Internal;

namespace GMap.NET.MapProviders.Google.Business
{
    /// <summary>
    /// A custom response factory which injects the custom cached HTTP responses
    /// </summary>
    public class GMapsCachedHttpGetResponseFactory : Http.HttpGetResponseFactory
    {
        public override HttpGetResponse CreateResponse(Uri uri, bool forceNoCache = false)
        {
            return new GMapsCachedHttpGetResponse(uri, forceNoCache);
        }
    }

    /// <summary>
    /// A cached http response
    /// </summary>
    public class GMapsCachedHttpGetResponse : HttpGetResponse
    {
        public static TimeSpan DefaultMaxCacheAge = TimeSpan.FromDays(20);


        public GMapsCachedHttpGetResponse(Uri uri, bool forceNoCache) 
            : base(uri, forceNoCache)
        {
        }

        public override string AsString()
        {
            var output = String.Empty;


            if (!DontUseCache)
            {
                output = GetContent(RequestUri, DefaultMaxCacheAge);
            }

            FromCache = !string.IsNullOrEmpty(output);

            if (!FromCache)
            {
                // fetch from the server and store in cache

                var response = WebRequest.Create(GetSignedUri()).GetResponse();

                if (response != null)
                {
                    var rs = response.GetResponseStream();
                    if (rs != null)
                    {
                        using (var reader = new StreamReader(rs))
                        {
                            output = reader.ReadToEnd();
                        }
                        SaveContent(RequestUri, output);
                    }
                }

            }

            return output;
        }

        #region GMaps Cache wrapper

        public static string GetContent(Uri request, TimeSpan cacheLifetime)
        {
            var content = Cache.Instance.GetContent(request.ToString(), CacheType.UrlCache, cacheLifetime);
            return content;
        }

        public static void SaveContent(Uri request, string responseContent)
        {
            Cache.Instance.SaveContent(request.ToString(), CacheType.UrlCache, responseContent);
        }

        #endregion
    }
}
