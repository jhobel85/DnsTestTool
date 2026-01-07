using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace SimpleDnsClient
{
    public class RestClient
    {
        public Guid SessionId { get; } = Guid.NewGuid();        
        private string serverUrl;
        private const string DNS_SERVER_PROCESS_NAME = "SimpleDnsServer";
        private const string DNS_SERVER_FOLDER = @"..\\SimpleDnsServer";
        private const string FRAMEWORK = "net8.0";

        public static void StartServer(string dnsServerPaths, String ip, int apiPort, int udpPort)
        {
            if (!IsDnsServerRunning())
            {
                RunDnsServer(dnsServerPaths, $"--ip {ip} --apiPort {apiPort} --udpPort {udpPort}");
                Console.WriteLine($"Dns server started.");
            }
            else
            {
                Console.WriteLine($"Dns server already running.");
            }
        }

        public static string GetServerExecPath(Dictionary<string, string> attributes)
        {
            string rootPath = attributes["Folder"].Replace(attributes["TestFolder"], "");
            return Path.Combine(rootPath, DNS_SERVER_FOLDER, FRAMEWORK, $"{DNS_SERVER_PROCESS_NAME}.exe");
        }


        public static void StartServer(string dnsServerPaths, int apiPort, int udpPort)
        {
            StartServer(dnsServerPaths, "0.0.0.0", apiPort, udpPort);
        }

        public RestClient(string ip, int apiPort)
        {
            this.serverUrl = BuildUrl(ip, apiPort);
            // Ensure /dns is always present as the base path
            if (!this.serverUrl.EndsWith("/dns"))
                this.serverUrl += "/dns";
        }

        public void Register(string domain, string ip, bool registerWithSessionContext = true)
        {
            if (registerWithSessionContext)
            {
                Register(domain, ip, SessionId.ToString());
            }
            else
            {
                Register(domain, ip);
            }
        }

        private void Register(string domain, string ip, string sessionId)
        {
            WebRequest request = WebRequest.Create($"{serverUrl}/register/session?domain={domain}&ip={ip}&sessionId={sessionId}");
            request.Method = "POST";
            request.ContentLength = 0;
            WebResponse response = request.GetResponse();
            HttpStatusCode statusCode = ((HttpWebResponse)response).StatusCode;

            if (statusCode != HttpStatusCode.OK)
            {
                Console.WriteLine($"Error while registering domain. HttpStatusCode={statusCode}");                
            }
            response.Close();
        }

        private void Register(string domain, string ip)
        {
            WebRequest request = WebRequest.Create($"{serverUrl}/register?domain={domain}&ip={ip}");
            request.Method = "POST";
            request.ContentLength = 0;
            WebResponse response = request.GetResponse();
            HttpStatusCode statusCode = ((HttpWebResponse)response).StatusCode;

            if (statusCode != HttpStatusCode.OK)
            {
                Console.WriteLine($"Error while registering domain. HttpStatusCode={statusCode}");
            }
            response.Close();
        }

        public string Resolve(string domain)
        {
            WebRequest request = WebRequest.Create($"{serverUrl}/resolve?domain={domain}");
            request.Method = "GET";
            WebResponse response = request.GetResponse();
            HttpStatusCode statusCode = ((HttpWebResponse)response).StatusCode;

            if (statusCode != HttpStatusCode.OK && statusCode != HttpStatusCode.NoContent)
            {     
                Console.WriteLine($"Error while resolving domain. HttpStatusCode={statusCode}");
            }

            string result = "";
            if (statusCode == HttpStatusCode.OK) result = GetResultFromResponse(response);

            response.Close();
            return result;
        }

        public void Unregister(string domain)
        {
            WebRequest request = WebRequest.Create($"{serverUrl}/unregister?domain={domain}");
            request.Method = "POST";
            request.ContentLength = 0;
            WebResponse response = request.GetResponse();
            HttpStatusCode statusCode = ((HttpWebResponse)response).StatusCode;

            if (statusCode != HttpStatusCode.OK && statusCode != HttpStatusCode.NoContent)
            {
                Console.WriteLine($"Error while unregistering domain. HttpStatusCode={statusCode}");
            }
            response.Close();
        }

        public void UregisterSession()
        {
            WebRequest request = WebRequest.Create($"{serverUrl}/unregister/session?sessionId={SessionId}");
            request.Method = "POST";
            request.ContentLength = 0;
            WebResponse response = request.GetResponse();
            HttpStatusCode statusCode = ((HttpWebResponse)response).StatusCode;

            if (statusCode != HttpStatusCode.OK && statusCode != HttpStatusCode.NoContent)
            {
                Console.WriteLine($"Error while unregistering domain. HttpStatusCode={statusCode}");                
            }
            response.Close();
        }

        public int SessionRecordsCount()
        {
            WebRequest request = WebRequest.Create($"{serverUrl}/count/session?sessionId={SessionId}");
            request.Method = "GET";
            request.ContentLength = 0;
            WebResponse response = request.GetResponse();
            HttpStatusCode statusCode = ((HttpWebResponse)response).StatusCode;

            if (statusCode != HttpStatusCode.OK && statusCode != HttpStatusCode.NoContent)
            {
                Console.WriteLine($"Error while get SessionRecordsCount. HttpStatusCode={statusCode}");
            }

            string result = "";
            if (statusCode == HttpStatusCode.OK) result = GetResultFromResponse(response);

            response.Close();
            return int.Parse(result);
        }

        public int RecordsCount()
        {
            WebRequest request = WebRequest.Create($"{serverUrl}/count");
            request.Method = "GET";
            request.ContentLength = 0;
            WebResponse response = request.GetResponse();
            HttpStatusCode statusCode = ((HttpWebResponse)response).StatusCode;

            if (statusCode != HttpStatusCode.OK)
            {
                Console.WriteLine($"Error while get RecordsCount. HttpStatusCode={statusCode}");                
            }
            string result = "";
            if (statusCode == HttpStatusCode.OK) result = GetResultFromResponse(response);

            response.Close();
            return int.Parse(result);
        }

        private string BuildUrl(string ip, int apiPort)
        {
            return $"http://{ip}:{apiPort}/dns";
        }

        private string GetResultFromResponse(WebResponse response)
        {
            using (Stream stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
        }

        private static bool IsDnsServerRunning()
        {
            foreach (var process in Process.GetProcesses())
            {
                if (process.ProcessName.Contains(DNS_SERVER_PROCESS_NAME))
                {
                    return true;
                }
            }
            return false;
        }

        private static void RunDnsServer(string exePath, string arguments)
        {            
            try
            {
                Process proc = Process.Start(exePath, arguments);    
                
                if (proc != null)
                {
                    Console.WriteLine("DNS Server succesfully started, process id " + proc.Id);
                }
                else
                {                    
                    Console.WriteLine("Error while starting DNS Server!");
                }
            }
            catch
            {
                HashSet<int> procIds = Utils.FindServerProcessIDs(53);
                if (procIds != null && procIds.Count > 0)
                {
                    string ids = String.Join(",", procIds);
                    Console.WriteLine("There is already exisiting process id(s) " + ids + " which use DNS port 53. Please check if TestStudio DnsServer is not running.");
                }
            }            
        }
    }
}
