using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Xunit;

namespace SimpleDnsServer.Tests
{
    public class DnsServerFixture : IDisposable
    {
        private Process _serverProcess;

        public DnsServerFixture()
        {
            // Kill any running DNS server before starting            
            SimpleDnsClient.Utils.KillAllServers(Constants.UdpPort, Constants.IP);
            SimpleDnsClient.Utils.KillAllServers(Constants.ApiPort, Constants.IP);

            // Path to the server executable
            // Go up from .../SimpleDnsTests/bin/Debug/netX.X/ to solution root
            var testBinDir = AppDomain.CurrentDomain.BaseDirectory;
            var solutionRoot = Directory.GetParent(testBinDir).Parent.Parent.Parent.Parent.FullName;
            var serverExe = Path.Combine(solutionRoot, "SimpleDnsServer", "bin", "Debug", "net8.0", "SimpleDnsServer.exe");
            if (!File.Exists(serverExe))
                throw new FileNotFoundException($"Could not find server executable at {serverExe}");

            var startInfo = new ProcessStartInfo
            {
                FileName = serverExe,
                Arguments = $"--ip {Constants.IP} --apiPort {Constants.ApiPort} --udpPort {Constants.UdpPort}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            _serverProcess = Process.Start(startInfo);
            if (_serverProcess == null)
                throw new InvalidOperationException($"Failed to start server process at {serverExe}");

            // Wait for server to start or exit
            Thread.Sleep(3000);
            if (_serverProcess.HasExited)
            {
                string stdOut = _serverProcess.StandardOutput.ReadToEnd();
                string stdErr = _serverProcess.StandardError.ReadToEnd();
                throw new InvalidOperationException($"Server process exited early.\nStdOut: {stdOut}\nStdErr: {stdErr}");
            }
        }

        public void Dispose()
        {
            // Kill any running DNS server after tests            
            SimpleDnsClient.Utils.KillAllServers(Constants.UdpPort, Constants.IP);
            SimpleDnsClient.Utils.KillAllServers(Constants.ApiPort, Constants.IP);
        }
    }
}
