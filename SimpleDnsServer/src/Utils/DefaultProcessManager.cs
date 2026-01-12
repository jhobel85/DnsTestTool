using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SimpleDnsServer.Utils;

public class DefaultProcessManager : IProcessManager
{
    public HashSet<int> FindServerProcessIDs(int portNr, string? ipAddress = null)
    {
        HashSet<int> ret = new HashSet<int>();
        string cmdArg = "/C netstat -ano | findstr \":" + portNr + "\"";
        var startInfo = new ProcessStartInfo()
        {
            Arguments = cmdArg.Trim(),
            FileName = @"cmd.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };
        Process cmd = new() { StartInfo = startInfo };
        string cmdError = string.Empty;
        try
        {
            cmd.Start();
            var stdOut = cmd.StandardOutput;
            var stdErr = cmd.StandardError;
            while (!stdOut.EndOfStream)
            {
                var line = stdOut.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                var lineClean = line.Replace("  ", " ");
                var splitResult = lineClean.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                if (splitResult.Length < 2)
                    continue;
                string strIpAndPort = splitResult[1];
                string? ip = null;
                string? port = null;
                int lastColon = strIpAndPort.LastIndexOf(':');
                if (lastColon > 0)
                {
                    port = strIpAndPort[(lastColon + 1)..];
                    ip = strIpAndPort[..lastColon];
                    if (ip.StartsWith("[") && ip.EndsWith("]"))
                        ip = ip[1..^1];
                }
                if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port))
                    continue;
                if (!int.TryParse(port, out int foundPortNr))
                    continue;
                string normIp = ip;
                string? normArgIp = ipAddress;
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    try { normIp = System.Net.IPAddress.Parse(ip).ToString(); } catch { }
                    try { normArgIp = System.Net.IPAddress.Parse(ipAddress).ToString(); } catch { }
                }
                if (foundPortNr == portNr && (ipAddress == null || normIp == normArgIp))
                {
                    string strProcessNr = splitResult[^1];
                    if (int.TryParse(strProcessNr, out int intProcessNr))
                        ret.Add(intProcessNr);
                }
            }
            cmdError = stdErr.ReadToEnd();
            cmd.WaitForExit();
            cmd.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        if (!string.IsNullOrEmpty(cmdError))
            Console.WriteLine("Process returned error: " + cmdError);
        return ret;
    }

    private static bool KillProcessAsAdmin(int pid)
    {
        bool ret = true;
        string arg = @"/c taskkill /f" + " /pid " + pid;
        try
        {
            ProcessStartInfo processInf = new("cmd")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas",
                Arguments = arg
            };
            var proc = Process.Start(processInf);
            if (proc != null && proc.HasExited)
                Console.WriteLine("Process ID " + pid + " killed by admin rights.");
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
            ret = false;
        }
        return ret;
    }

    public bool KillAllServers(int portNr, string? ipAddress = null)
    {
        bool ret = true;
        foreach (var procId in FindServerProcessIDs(portNr, ipAddress))
            ret &= KillProcessAsAdmin(procId);
        return ret;
    }

    public bool IsServerRunning(int portNr, string? ipAddress = null)
    {
        var procesIds = FindServerProcessIDs(portNr, ipAddress);
        return procesIds.Count > 0;
    }
}
