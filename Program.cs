using System;
using System.Text;
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
        static string m_clientRestDocService = "/LATEST/search"; // MarkLogic REST API service for document search.
        static string m_clientRestEvalService = "/LATEST/eval"; // MarkLogic REST API service for eval code.
        static string m_host = "";
        static string m_port = "";
        static HttpClient m_httpClient;

        static void Main(string[] args)
        {
            m_host = ConfigurationManager.AppSettings["host"];
            m_port = ConfigurationManager.AppSettings["port"];
            var username = ConfigurationManager.AppSettings["username"];
            var password = ConfigurationManager.AppSettings["password"];
            var realm = ConfigurationManager.AppSettings["realm"];

            // create connection
            CredentialCache m_credCache = new CredentialCache();
            HttpClientHandler m_clientHandler = new HttpClientHandler();
            string mlHost = string.Format("http://{0}:{1}", m_host, m_port);
            m_credCache.Add(new Uri(mlHost), "Digest", new NetworkCredential(username, password, realm));
            m_clientHandler.Credentials = m_credCache;
            m_clientHandler.PreAuthenticate = true;
            m_httpClient = new HttpClient(m_clientHandler);

            // read parameters
            string service = args[0];
            string param = args[1];
            string result = "";
            switch (service)
            {
                case "search":
                    result = serviceSearch(param);
                    break;
                case "eval":
                    result = serviceEval(param);
                    break;
            }
            Console.WriteLine(result);
        }

        static private string serviceSearch(string _text)
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

        static private string serviceEval(string _file)
        {
            // build url
            string url = string.Format("http://{0}:{1}{2}",
                                        m_host, m_port, m_clientRestEvalService);
            Uri requestUri = new Uri(url);
            var request = new HttpRequestMessage(new HttpMethod("POST"), requestUri);
            request.Content = new StringContent(Regex.Replace(File.ReadAllText(_file), "(?:\\r\\n|\\n|\\r)", string.Empty));
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

            // call url
            HttpResponseMessage response = m_httpClient.SendAsync(request).Result;
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

    }
}
