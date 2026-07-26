using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Management;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace RED_X_CLOUD_CONTROL_BASIC
{
    internal class KeyAuth
    {
        private readonly string name;
        private readonly string ownerid;
        private readonly string version;
        private readonly string url = "https://keyauth.win/api/1.2/";

        private string sessionid;
        private bool initialized = false;

        public string Message { get; private set; } = "";
        public bool Success { get; private set; } = false;

        public KeyAuth(string name, string ownerid, string version)
        {
            this.name = name;
            this.ownerid = ownerid;
            this.version = version;
        }

        private string GetHWID()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive");
                var ids = new List<string>();
                foreach (ManagementObject obj in searcher.Get())
                {
                    var serial = obj["SerialNumber"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(serial))
                        ids.Add(serial.Trim());
                }
                string raw = string.Join("", ids);
                if (string.IsNullOrEmpty(raw))
                    raw = Environment.MachineName + Environment.UserName;

                using (var sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
            catch
            {
                using (var sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName + Environment.UserName));
                    return BitConverter.ToString(hash).Replace("-", "").ToLower();
                }
            }
        }

        public bool Init()
        {
            try
            {
                var data = new NameValueCollection
                {
                    { "type", "init" },
                    { "ver", version },
                    { "name", name },
                    { "ownerid", ownerid }
                };

                string response = Post(data);
                var json = ParseJson(response);

                json.TryGetValue("success", out string success);
                json.TryGetValue("sessionid", out string sid);
                json.TryGetValue("message", out string msg);

                Message = msg ?? "";

                if (success == "true" && !string.IsNullOrEmpty(sid))
                {
                    sessionid = sid;
                    initialized = true;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return false;
            }
        }

        public bool Login(string username, string password)
        {
            if (!initialized) return false;
            try
            {
                var data = new NameValueCollection
                {
                    { "type", "login" },
                    { "username", username },
                    { "pass", password },
                    { "hwid", GetHWID() },
                    { "sessionid", sessionid },
                    { "name", name },
                    { "ownerid", ownerid }
                };

                string response = Post(data);
                var json = ParseJson(response);

                json.TryGetValue("success", out string success);
                json.TryGetValue("message", out string msg);

                Message = msg ?? "";
                Success = success == "true";
                return Success;
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return false;
            }
        }

        public bool License(string key)
        {
            if (!initialized) return false;
            try
            {
                var data = new NameValueCollection
                {
                    { "type", "license" },
                    { "key", key },
                    { "hwid", GetHWID() },
                    { "sessionid", sessionid },
                    { "name", name },
                    { "ownerid", ownerid }
                };

                string response = Post(data);
                var json = ParseJson(response);

                json.TryGetValue("success", out string success);
                json.TryGetValue("message", out string msg);

                Message = msg ?? "";
                Success = success == "true";
                return Success;
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return false;
            }
        }

        private string Post(NameValueCollection data)
        {
            using (var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                byte[] result = client.UploadValues(url, "POST", data);
                return Encoding.UTF8.GetString(result);
            }
        }

        private Dictionary<string, string> ParseJson(string json)
        {
            var dict = new Dictionary<string, string>();
            json = json.Trim().TrimStart('{').TrimEnd('}');

            int i = 0;
            while (i < json.Length)
            {
                while (i < json.Length && json[i] != '"') i++;
                if (i >= json.Length) break;
                i++;
                int kStart = i;
                while (i < json.Length && json[i] != '"') i++;
                string key = json.Substring(kStart, i - kStart);
                i++;

                while (i < json.Length && json[i] != ':') i++;
                i++;
                while (i < json.Length && json[i] == ' ') i++;

                string value;
                if (i < json.Length && json[i] == '"')
                {
                    i++;
                    int vStart = i;
                    while (i < json.Length && json[i] != '"') i++;
                    value = json.Substring(vStart, i - vStart);
                    i++;
                }
                else
                {
                    int vStart = i;
                    while (i < json.Length && json[i] != ',' && json[i] != '}') i++;
                    value = json.Substring(vStart, i - vStart).Trim();
                }

                if (!string.IsNullOrEmpty(key))
                    dict[key] = value;

                while (i < json.Length && json[i] != '"' && json[i] != '}') i++;
            }

            return dict;
        }
    }
}
