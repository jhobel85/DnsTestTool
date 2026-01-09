using System.Diagnostics;
using static SimpleDnsServer.DnsConst;

namespace SimpleDnsServer.Utils;

public class ServerUtils
{
    private static string GetServerExecutablePath()
    {
        var testBinDir = AppDomain.CurrentDomain.BaseDirectory; // SimpleDnsTests/bin/Debug/net8.0
                                                                // Safely traverse up 4 parent directories to reach solution root
        var dir = new DirectoryInfo(testBinDir);
        for (int i = 0; i < 4; i++)
        {
            if (dir.Parent == null)
                throw new DirectoryNotFoundException($"Could not find solution root from testBinDir. Problem at: {dir.FullName}");
            dir = dir.Parent;
        }
        var solutionRoot = dir.FullName; // to solution root (SimpleDnsTestTool)
        string proc_name = DNS_SERVER_PROCESS_NAME + ".exe";
        var ret = Path.Combine(solutionRoot, "SimpleDnsServer", "bin", "Debug", FRAMEWORK, proc_name);
        if (!File.Exists(ret))
            throw new FileNotFoundException($"Could not find server executable at {ret}");
        return ret;
    }

    public static void StartDnsServer()
    {
        var serverExe = GetServerExecutablePath();
        var ip = GetDnsIp(DnsIpMode.Localhost, null);
        var ip6 = GetDnsIpV6(DnsIpMode.Localhost, null);
        StartDnsServer(serverExe, ip, ip6, ApiPort, UdpPort);
    }

    public static void StartDnsServer(String ip, String ip6, int apiPort, int udpPort)
    {
        var serverExe = GetServerExecutablePath();
        StartDnsServer(serverExe, ip, ip6, apiPort, udpPort);
    }

    public static void StartDnsServer(string serverExe, String ip, String ip6, int apiPort, int udpPort)
    {
        var _serverProcess = Process.Start(new ProcessStartInfo
        {
            FileName = serverExe,
            Arguments = $"--ip {ip} --ip6 {ip6} --apiPort {apiPort} --udpPort {udpPort}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });

        // Wait for server to start or exit
        Thread.Sleep(3000);
        if (_serverProcess == null)
            throw new InvalidOperationException("Failed to start DNS server process.");

        if (_serverProcess.HasExited)
        {
            string stdOut = _serverProcess.StandardOutput.ReadToEnd();
            string stdErr = _serverProcess.StandardError.ReadToEnd();
            throw new InvalidOperationException($"Server process exited early.\nStdOut: {stdOut}\nStdErr: {stdErr}");
        }

        Console.WriteLine("DNS Server succesfully started, process id " + _serverProcess.Id);
    }
}
