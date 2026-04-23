using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace BitBing.UnityBridge.Editor.Settings
{
    /// <summary>
    /// Persistent settings for the Unity Bridge.
    /// Stored as ScriptableObject at Assets/Settings/BridgeSettings.asset
    /// </summary>
    public class BridgeSettings : ScriptableObject
    {
        public enum TransportMode { Tcp, NamedPipe, Mcp }

        [SerializeField] private TransportMode _transportMode = TransportMode.Mcp;
        [SerializeField] private int _tcpPort = 57432;
        [SerializeField] private string _pipeName = "bitbing-unity";
        [SerializeField] private int _mcpPort = 8080;
        [SerializeField] private bool _autoConnect = true;
        [SerializeField] private bool _logVerbose = false;
        [SerializeField] private int _timeoutMs = 10000;
        [SerializeField] private string _screenshotDir = "Temp/AgentScreenshots";

        public TransportMode transportMode => _transportMode;
        public int tcpPort => _tcpPort;
        public string pipeName => _pipeName;
        public int mcpPort => _mcpPort;
        public bool autoConnect => _autoConnect;
        public bool logVerbose => _logVerbose;
        public int timeoutMs => _timeoutMs;
        public string screenshotDir => _screenshotDir;

        private const string AssetPath = "Assets/Settings/BridgeSettings.asset";
        private static BridgeSettings s_instance;

        public static BridgeSettings GetOrCreate()
        {
            if (s_instance != null) return s_instance;

            s_instance = AssetDatabase.LoadAssetAtPath<BridgeSettings>(AssetPath);
            if (s_instance != null) return s_instance;

            var guids = AssetDatabase.FindAssets("t:BridgeSettings");
            if (guids.Length > 0)
            {
                s_instance = AssetDatabase.LoadAssetAtPath<BridgeSettings>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
                if (s_instance != null) return s_instance;
            }

            var dir = Path.GetDirectoryName(AssetPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            s_instance = CreateInstance<BridgeSettings>();
            AssetDatabase.CreateAsset(s_instance, AssetPath);
            AssetDatabase.SaveAssets();
            return s_instance;
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}
