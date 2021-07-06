using AngleSharp.Html.Parser;
using AppStoreParser.Constants;
using AppStoreParser.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
namespace AppStoreParser
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Write up bound");
            var upperIndex = int.Parse(Console.ReadLine());


            Console.WriteLine("Write bottom bound");
            var bottomIndex = int.Parse(Console.ReadLine());

            Console.WriteLine("You want to clear previous results?");
            var clear = Console.ReadLine();
            if (clear.Contains("y"))
            {
                File.Delete(FileConstants.FILE_LOCATION);
                File.Delete(FileConstants.NOT_FOUND_LOCATION);
                File.Delete(FileConstants.PROXY_FILE_LOCATION);
            }
            BinaryFormatter bf = new BinaryFormatter();
            List<string> missedLocations = new List<string>();
            if (File.Exists(FileConstants.NOT_FOUND_LOCATION))
            {
                using (FileStream fs = new FileStream(FileConstants.NOT_FOUND_LOCATION, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    missedLocations = (List<string>)bf.Deserialize(fs);

                }
            }
            var rand = new Random();
            var parser = new HtmlParser();
            var proxyHelper = new ProxyHelper().GenerateProxy().Build();
            using (FileStream fs = new FileStream(FileConstants.FILE_LOCATION, FileMode.OpenOrCreate, FileAccess.ReadWrite))

            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    for (; bottomIndex < upperIndex; upperIndex--)
                    {
                        var currentUrl = WebConstants.URL_APP_STORE+ upperIndex.ToString();
                        try
                        {
                            WebRequest WR = WebRequest.Create(currentUrl);
                            WR.Method = "GET";
                            if (proxyHelper.Count > 0)
                            { 
                                    string[] fulladress = proxyHelper[rand.Next(0, proxyHelper.Count-1)].Split(":");
                                    var (adress, port) = (fulladress[0], int.Parse(fulladress[1]));
                                    WebProxy prox = new WebProxy(adress, port);
                                    prox.BypassProxyOnLocal = false;
                                    WR.Proxy = prox;
                                
                            }
                            WR.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:65.0) Gecko/20100101 Firefox/65.0");
                            WebResponse response = WR.GetResponse();
                            string html;
                            using (Stream stream = response.GetResponseStream())
                            {
                                using (StreamReader reader = new StreamReader(stream))
                                {
                                    html = reader.ReadToEnd();
                                }
                            }

                            var result = parser.ParseDocument(html);
                            var divs = result.GetElementsByClassName("information-list__item__definition");
                            if (divs != null && divs.Length > 0)
                            {
                                if (divs.FirstOrDefault(f => f.TextContent.ToLower().Contains("game")) != null)
                                {
                                    sw.WriteLine(currentUrl);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }

                        missedLocations.Add(currentUrl);
                        Thread.Sleep(rand.Next(2000, 5000));
                    }
                }

            }
            using (FileStream fs = new FileStream(FileConstants.NOT_FOUND_LOCATION, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                bf.Serialize(fs, missedLocations);

            }
        }
    }
}
