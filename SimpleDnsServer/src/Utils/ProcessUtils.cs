using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleDnsServer.Utils
{
    public class ProcessUtils
    {
        [Obsolete("Use IProcessManager and DefaultProcessManager instead.")]
        public static HashSet<int> FindServerProcessIDs(int portNr, string? ipAddress = null)
            => new DefaultProcessManager().FindServerProcessIDs(portNr, ipAddress);

        [Obsolete("Use IProcessManager and DefaultProcessManager instead.")]
        public static bool KillAllServers(int portNr, string? ipAddress = null)
            => new DefaultProcessManager().KillAllServers(portNr, ipAddress);

        [Obsolete("Use IProcessManager and DefaultProcessManager instead.")]
        public static bool IsServerRunning(int portNr, string? ipAddress = null)
            => new DefaultProcessManager().IsServerRunning(portNr, ipAddress);
    }
}
