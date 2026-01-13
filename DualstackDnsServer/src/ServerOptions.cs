namespace DualstackDnsServer;

public class ServerOptions
{
    public string Ip { get; set; } = "";// can also be hostname e.g. , localhost or ::1
    public string IpV6 { get; set; } = "";// can also be hostname e.g. , localhost or ::1
    public int ApiPort { get; set; }
    public int UdpPort { get; set; }
    public string CertPath { get; set; } = "";
    public string CertPassword { get; set; } = "";
    public bool EnableHttp { get; set; }
    public string[] Args { get; set; } = [];
}
