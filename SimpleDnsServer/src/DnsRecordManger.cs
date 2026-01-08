#nullable enable
namespace SimpleDnsServer
{
    public class DnsRecordManger
    {
        private readonly Dictionary<string, string> records = [];
        private readonly Dictionary<string, List<string>> sessions = [];

        public DnsRecordManger() => Console.WriteLine("Server new instance");

        public void Register(string domain, string ip, string? sessionId = null)
        {
            records[domain] = ip;
            if (sessionId == null)
                return;
            AddSessionItem(sessionId, domain);
        }

        public void Unregister(string domain) => records.Remove(domain);

        public string? Resolve(string domain)
        {
            foreach (string key in records.Keys)
            {
                if (domain.StartsWith(key))
                {
                    string str = domain.Substring(key.Length);
                    if (str.Length == 0 || str.StartsWith("."))
                        return records[key];
                }                                
            }
            return null;
        }

        public int GetCount() => this.records.Count;

        public int GetSessionCount(string sessionId)
        {
            return !sessions.TryGetValue(sessionId, out List<string>? value) ? 0 : value.Count;
        }

        public void UnregisterSession(string sessionId)
        {
            if (!sessions.TryGetValue(sessionId, out List<string>? value))
                return;
            foreach (string key in value)
                records.Remove(key);
            sessions.Remove(sessionId);
        }

        public void UnregisterAll() => records.Clear();

        private void AddSessionItem(string key, string domain)
        {
            if (sessions.TryGetValue(key, out List<string>? value))
                value.Add(domain);
            else
                sessions[key] = new List<string>() { domain };
        }
    }
}
