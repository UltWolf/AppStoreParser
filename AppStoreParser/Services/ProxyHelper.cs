using AngleSharp.Html.Parser;
using AppStoreParser.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

namespace AppStoreParser.Services
{
    class ProxyHelper
    {
        private bool IsGenerate;

        public ProxyHelper GenerateProxy()
        {
            ProxyServerConnectionManagment.GenerateProxy();
            IsGenerate = true;
            return this;
        }

        public List<string> Build()
        {
            if (IsGenerate)
                return ProxyServerConnectionManagment.GetUrlsList();
            else
            {
                throw new Exception("Urls didn`t generate");
            }
        }
        protected static class ProxyServerConnectionManagment
        {
            private static BinaryFormatter bf;

            private static List<string> Urls = new List<string>();
            private readonly static object locker = new object();


            public static List<string> GetUrlsList()
            {
                lock (locker)
                {
                    if (Urls.Count != 0)
                    {

                        return Urls;
                    }
                    else
                    {
                        return DeserialiseUrl();
                    }
                }
            }

            private static void SerialiseProxyServersUA(bool IsForce)
            {
                if (File.Exists("proxyList.txt") || IsForce)
                {
                    List<String> proxyList = new List<string>();
                    var httpRequest = (HttpWebRequest)HttpWebRequest.Create("https://www.proxynova.com/proxy-server-list/country-ua/");
                    httpRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                    httpRequest.KeepAlive = true;
                    httpRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:65.0) Gecko/20100101 Firefox/65.0";

                    var httpResponse = httpRequest.GetResponse();
                    string html = "";
                    using (Stream stream = httpResponse.GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(stream))
                        {
                            html = sr.ReadToEnd();
                        }
                    }
                    var parser = new HtmlParser();
                    var body = parser.ParseDocument(html);
                    var table = body.GetElementById("tbl_proxy_list");

                    var trs = table.GetElementsByTagName("tr");
                    for (int i = 1; i < trs.Length; i++)
                    {
                        var tds = trs[i].GetElementsByTagName("td");
                        if (tds.Length > 0)
                        {
                            var proxyScript = tds[0].GetElementsByTagName("script");
                            if (proxyScript.Length != 0)
                            {
                                var arrayWithData = proxyScript[0].TextContent.Split("\'");
                                if (arrayWithData.Length > 4)
                                {
                                    var numb = arrayWithData[1].Substring(8);
                                    var lastDataNumbs = arrayWithData[3];
                                    string http = numb + lastDataNumbs;
                                    string port = tds[1].TextContent.Replace("\t", "").Replace("\n", "").Replace(" ", "");
                                    proxyList.Add(http + ":" + port);
                                }
                            }
                        }

                    }
                    using (FileStream fs = new FileStream(FileConstants.PROXY_FILE_LOCATION, FileMode.Create, FileAccess.Write))
                    {

                        bf = new BinaryFormatter();
                        bf.Serialize(fs, proxyList);
                    }
                }
            }
            public static void SerializeByPuts(List<string> hrefs)
            {
                using (FileStream fs = new FileStream(FileConstants.PROXY_FILE_LOCATION, FileMode.Create, FileAccess.Write))
                {

                    bf = new BinaryFormatter();
                    bf.Serialize(fs, hrefs);
                }
            }

            public static void GenerateProxy()
            {
                if (!File.Exists("proxyList.txt"))
                {
                    SerialiseProxyServersUA(true);
                }

                if (Urls.Count == 0)
                {
                    SetUrls();
                }
            }

            private static void SetUrls()
            {

                lock (locker)
                {
                    Urls = DeserialiseUrl();
                }

            }

            private static List<string> DeserialiseUrl()
            {
                using (FileStream fs = new FileStream("proxyList.txt", FileMode.Open, FileAccess.Read))
                {
                    bf = new BinaryFormatter();
                    return (List<string>)bf.Deserialize(fs);
                }

            }
        }
    }
}
