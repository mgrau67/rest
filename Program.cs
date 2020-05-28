using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace rest
{
    class Program
    {
        const string m_clientRestDocService = "/LATEST/search"; // MarkLogic REST API service for document search.
        const string m_clientRestEvalService = "/LATEST/eval"; // MarkLogic REST API service for eval code.
        static string m_host = "";
        static string m_port = "";
        static HttpClient m_httpClient;
        const string cContentType = "Content-Type";
        const string cTextPlain = "text/plain";
        const string cApplicationXml = "application/xml";
        const string cSeparator = "--";


        static void Main(string[] args)
        {
            m_host = ConfigurationManager.AppSettings["host"];
            m_port = ConfigurationManager.AppSettings["port"];
            string username = ConfigurationManager.AppSettings["username"];
            string password = ConfigurationManager.AppSettings["password"];
            string realm = ConfigurationManager.AppSettings["realm"];

            // create connection
            CredentialCache m_credCache = new CredentialCache();
            HttpClientHandler m_clientHandler = new HttpClientHandler();
            string mlHost = string.Format("http://{0}:{1}", m_host, m_port);
            m_credCache.Add(new Uri(mlHost), "Digest", new NetworkCredential(username, password, realm));
            m_clientHandler.Credentials = m_credCache;
            m_clientHandler.PreAuthenticate = true;
            m_httpClient = new HttpClient(m_clientHandler);

            // read parameters
            if (args.Length < 2)
            {
                Console.WriteLine("Params: <search/eval> <text/file-input> <file-output>");
                return;
            }
            string service = args[0];
            string param = args[1];
            string result = "";
            switch (service)
            {
                case "search":
                    result = ServiceSearch(param);
                    break;
                case "eval":
                    result = ServiceEval(param);
                    if (args.Length >= 3) File.WriteAllText(args[2], result);
                    break;
            }
            Console.WriteLine(result);
        }

        static private string ServiceSearch(string _text)
        {
            // build url
            string query = _text;
            int start = 1;
            int pageLength = 10;
            string format = "xml";
            string url = string.Format("http://{0}:{1}{2}?q={3}&start={4}&pageLength={5}&format={6}",
                                       m_host, m_port, m_clientRestDocService,
                                       query, start, pageLength, format);            
            Uri requestUri = new Uri(url);

            // call url
            HttpResponseMessage response = m_httpClient.GetAsync(requestUri).Result;
            string result = "";
            if (response.IsSuccessStatusCode)
            {
                result = response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                result = string.Format("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }
            return result;
        }

        static private string ServiceEval(string _file)
        {
            // build url
            string url = string.Format("http://{0}:{1}{2}",
                                        m_host, m_port, m_clientRestEvalService);
            Uri requestUri = new Uri(url);
            var request = new HttpRequestMessage(new HttpMethod("POST"), requestUri);
            string content = File.ReadAllText(_file);
            //content = WebUtility.UrlEncode(content); %d* error!!!
            request.Content = new StringContent(Regex.Replace(content, "(?:\\r\\n|\\n|\\r)", string.Empty));
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

            // call url
            HttpResponseMessage response = m_httpClient.SendAsync(request).Result;
            string result = "";
            if (response.IsSuccessStatusCode)
            {
                result = response.Content.ReadAsStringAsync().Result;
                result = ParseEval(result);
            }
            else
            {
                result = string.Format("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }
            return result;
        }

        /// <summary>
        /// Parse content return by eval service
        /// </summary>
        /// <param name="_response"></param>
        /// <returns></returns>
        static string ParseEval(string _response)
        {
            if (ConfigurationManager.AppSettings["parse-eval"] == "0") return _response;

            string result = "";
            // loop lines
            using (StringReader reader = new StringReader(_response))
            {
                string line, separator = cSeparator;
                bool addLine = false;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.IndexOf(separator, 0) >= 0)
                    {
                        separator += line.Substring(separator.Length) + cSeparator;
                        addLine = false;
                    }
                    else if (line.IndexOf(cContentType, 0) >= 0)
                    {
                        string[] arr = line.Split(':');
                        string value = (arr.Length > 0) ? arr[1].Trim() : string.Empty;
                        switch (value)
                        {
                            case cTextPlain:
                                // ignore lines begining X-
                                while (reader.ReadLine().IndexOf("X-", 0) >= 0) { }
                                line = reader.ReadLine();
                                addLine = true;
                                break;
                            case cApplicationXml:
                                // ignore lines begining X-
                                while (reader.ReadLine().IndexOf("X-", 0) >= 0) { }
                                line = reader.ReadLine();
                                addLine = true;
                                break;
                        }
                    }
                    if (addLine) result += line;
                }
            }
            return result;
        }

    }
}
