using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleDnsClient
{
    public class Utils
    {
        public static HashSet<int> FindServerProcessIDs(int portNr)
        {
            HashSet<int> ret = new HashSet<int>();
            //Using the /C argument, you can give it the command what you want to execute
            string cmdArg = "/C netstat -ano | findstr \":" + portNr + "\"";

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                Arguments = cmdArg.Trim(),
                FileName = @"cmd.exe",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false//process is started as a child process
            };

            Process cmd = new Process { StartInfo = startInfo };
            string cmdError = string.Empty;
            try
            {
                cmd.Start();
                var stdOut = cmd.StandardOutput;
                var stdErr = cmd.StandardError;

                while (!stdOut.EndOfStream)
                {
                    var line = stdOut.ReadLine();
                    var lineClean = line?.Replace("  ", " ");
                    var splitResult = lineClean?.Split(" ");
                    if (splitResult != null && splitResult.Length > 0)
                    {
                        string strIpAndPort = splitResult[1];
                        string port = strIpAndPort.Split(":")[1];
                        _ = int.TryParse(port, out int foundPortNr);
                        if (foundPortNr == portNr)
                        {
                            string strProcessNr = splitResult[splitResult.Length - 1];
                            _ = int.TryParse(strProcessNr, out int intProcessNr);                            
                            ret.Add((int)intProcessNr);                            
                        }
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

            if (cmdError != null && cmdError.Length > 0)
            {
                Console.WriteLine("Process returned error: " + cmdError);
            }

            return ret;
        }

        private static bool KillProcessAsAdmin(int pid)
        {
            bool ret = true;
            string arg = @"/c taskkill /f" + " /pid " + pid;
            try
            {
                //Turning UAC off (run as admin) and kill process to avoid error: Access is denied
                ProcessStartInfo processInf = new ProcessStartInfo("cmd")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas",
                    Arguments = arg
                };
                var proc = Process.Start(processInf);
                if (proc != null && proc.HasExited)
                {
                    Console.WriteLine("Process ID " + pid + " killed by admin rights.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                ret = false;
            }

            return ret;
        }

        public static bool KillAllServers(int portNr)
        {
            bool ret = true;
            foreach (var procId in FindServerProcessIDs(portNr))
            {
                ret &= KillProcessAsAdmin(procId);
            }

            return ret;
        }

        public static bool IsServerRunning(int portNr)
        {
            var procesIds = FindServerProcessIDs(portNr);
            var ret = procesIds.Count > 0;
            return ret;
        }
    }
}
