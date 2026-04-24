using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

using Newtonsoft.Json;

namespace BitBing.UnityBridge.Editor.Helpers
{
    /// <summary>
    /// Dynamic port allocation + persistent registry, adapted from COPLAY's PortManager.
    /// Unity bridge (8080) and Python chat server (8001) share this registry so the
    /// Python side can discover the actual ports Unity ended up using.
    ///
    /// Registry path: %TEMP%/bitbing/ports.json
    /// </summary>
    public static class PortManager
    {
        public const int DefaultUnityPort = 8080;
        public const int DefaultChatPort = 8001;
        private const int MaxPortAttempts = 100;
        private const string RegistryDir = "bitbing";
        private const string RegistryFile = "ports.json";

        [Serializable]
        public class PortRegistry
        {
            public int unity_port;
            public int chat_port;
            public string project_path;
            public string updated_at;
        }

        public static string RegistryPath
        {
            get
            {
                var dir = Path.Combine(Path.GetTempPath(), RegistryDir);
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, RegistryFile);
            }
        }

        public static int GetUnityPortWithFallback()
        {
            var stored = ReadRegistry();
            if (stored != null && stored.unity_port > 0 &&
                string.Equals(stored.project_path ?? "", Application.dataPath ?? "", StringComparison.OrdinalIgnoreCase))
            {
                return stored.unity_port;
            }
            return DefaultUnityPort;
        }

        public static int DiscoverUnityPort()
        {
            int port = FindAvailablePort(DefaultUnityPort);
            SaveUnityPort(port);
            return port;
        }

        public static void SaveUnityPort(int port)
        {
            var reg = ReadRegistry() ?? new PortRegistry();
            reg.unity_port = port;
            reg.project_path = Application.dataPath;
            reg.updated_at = DateTime.UtcNow.ToString("o");
            WriteRegistry(reg);
        }

        public static void SaveChatPort(int port)
        {
            var reg = ReadRegistry() ?? new PortRegistry();
            reg.chat_port = port;
            reg.project_path = Application.dataPath;
            reg.updated_at = DateTime.UtcNow.ToString("o");
            WriteRegistry(reg);
        }

        public static bool IsPortAvailable(int port)
        {
            try
            {
                var listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                listener.Stop();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
        }

        private static int FindAvailablePort(int start)
        {
            for (int i = 0; i < MaxPortAttempts; i++)
            {
                int p = start + i;
                if (IsPortAvailable(p)) return p;
            }
            throw new InvalidOperationException(
                $"No available port in range {start}..{start + MaxPortAttempts}");
        }

        private static PortRegistry ReadRegistry()
        {
            try
            {
                if (!File.Exists(RegistryPath)) return null;
                return JsonConvert.DeserializeObject<PortRegistry>(File.ReadAllText(RegistryPath));
            }
            catch
            {
                return null;
            }
        }

        private static void WriteRegistry(PortRegistry reg)
        {
            try
            {
                File.WriteAllText(RegistryPath, JsonConvert.SerializeObject(reg, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PortManager] Failed to write registry: {ex.Message}");
            }
        }
    }
}
