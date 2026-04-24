using System;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace BitBing.UnityBridge.Editor.Helpers
{
    /// <summary>
    /// PID-file lifecycle for the Python chat server. Adapted from COPLAY's PidFileManager.
    /// Lets us avoid spawning a second server when one is already running.
    ///
    /// PID file: %TEMP%/bitbing/chat-server.pid
    /// </summary>
    public static class PidFileManager
    {
        private const string PidFileName = "chat-server.pid";

        public static string PidPath
        {
            get
            {
                var dir = Path.Combine(Path.GetTempPath(), "bitbing");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, PidFileName);
            }
        }

        public static void Write(int pid)
        {
            try { File.WriteAllText(PidPath, pid.ToString()); }
            catch (Exception ex) { Debug.LogWarning($"[PidFileManager] write failed: {ex.Message}"); }
        }

        public static int? Read()
        {
            try
            {
                if (!File.Exists(PidPath)) return null;
                if (int.TryParse(File.ReadAllText(PidPath).Trim(), out int pid)) return pid;
            }
            catch { }
            return null;
        }

        public static bool IsAlive(int pid)
        {
            try
            {
                var p = Process.GetProcessById(pid);
                return p != null && !p.HasExited;
            }
            catch { return false; }
        }

        public static void Clear()
        {
            try { if (File.Exists(PidPath)) File.Delete(PidPath); } catch { }
        }
    }
}
