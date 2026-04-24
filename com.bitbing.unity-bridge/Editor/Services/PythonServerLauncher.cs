using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using BitBing.UnityBridge.Editor.Helpers;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BitBing.UnityBridge.Editor.Services
{
    /// <summary>
    /// Robust Python chat server launcher, adapted from COPLAY's
    /// ServerCommandBuilder + ProcessDetector. Handles:
    ///   - PID-file check (don't double-start)
    ///   - Dynamic port allocation
    ///   - Locating unity-mcp-server/ via parent walk
    ///   - PYTHONPATH=src python -m uvicorn fallback
    /// </summary>
    public static class PythonServerLauncher
    {
        public class LaunchResult
        {
            public bool Success;
            public int Port;
            public int? Pid;
            public string Message;
        }

        public static async Task<LaunchResult> EnsureRunningAsync()
        {
            int port = PortManager.DefaultChatPort;

            // 1) Already running on default port?
            if (await IsPortRespondingAsync(port))
            {
                return new LaunchResult { Success = true, Port = port, Message = "Already running" };
            }

            // 2) PID-file says a server should exist — but didn't respond above. Clear stale.
            var existingPid = PidFileManager.Read();
            if (existingPid.HasValue && !PidFileManager.IsAlive(existingPid.Value))
            {
                PidFileManager.Clear();
            }

            // 3) Pick a free port (might bump off 8001 if collision)
            if (!PortManager.IsPortAvailable(port))
            {
                BitBingLog.Warn("PythonLauncher", $"Port {port} busy, searching alternative…");
                for (int p = port + 1; p < port + 50; p++)
                {
                    if (PortManager.IsPortAvailable(p)) { port = p; break; }
                }
            }
            PortManager.SaveChatPort(port);

            // 4) Build command
            var serverDir = FindServerDir();
            if (serverDir == null)
            {
                return new LaunchResult { Success = false, Message = "unity-mcp-server/ not found" };
            }

            var psi = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"-m uvicorn unity_mcp_server.chat_server:app --host 127.0.0.1 --port {port}",
                WorkingDirectory = serverDir,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.EnvironmentVariables["PYTHONPATH"] = Path.Combine(serverDir, "src");

            try
            {
                var proc = Process.Start(psi);
                if (proc == null)
                {
                    return new LaunchResult { Success = false, Message = "Process.Start returned null" };
                }
                PidFileManager.Write(proc.Id);
                BitBingLog.Info("PythonLauncher", $"Started PID {proc.Id} on port {port}");

                // Wait up to 5s for the port to respond
                for (int i = 0; i < 25; i++)
                {
                    await Task.Delay(200);
                    if (await IsPortRespondingAsync(port))
                    {
                        return new LaunchResult { Success = true, Port = port, Pid = proc.Id, Message = "Started" };
                    }
                }
                return new LaunchResult { Success = false, Port = port, Pid = proc.Id, Message = "Started but not responding" };
            }
            catch (Exception ex)
            {
                return new LaunchResult { Success = false, Message = $"Launch failed: {ex.Message}" };
            }
        }

        private static string FindServerDir()
        {
            try
            {
                var dir = new DirectoryInfo(Application.dataPath);
                for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
                {
                    var candidate = Path.Combine(dir.FullName, "unity-mcp-server");
                    if (File.Exists(Path.Combine(candidate, "pyproject.toml"))) return candidate;
                }
            }
            catch { }
            return null;
        }

        private static async Task<bool> IsPortRespondingAsync(int port)
        {
            try
            {
                using var client = new TcpClient();
                var connect = client.ConnectAsync("127.0.0.1", port);
                var done = await Task.WhenAny(connect, Task.Delay(500));
                return done == connect && client.Connected;
            }
            catch { return false; }
        }
    }
}
